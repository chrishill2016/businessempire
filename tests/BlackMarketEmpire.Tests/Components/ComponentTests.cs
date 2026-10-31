using BlackMarketEmpire.Features.Game;
using BlackMarketEmpire.Features.Scores;
using BlackMarketEmpire.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace BlackMarketEmpire.Tests.Components;

public class ComponentTests : BunitContext
{
    public ComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void StatusPanel_shows_day_city_cash_debt_and_space()
    {
        var state = TestGame.Engine().NewGame();

        var cut = Render<StatusPanel>(p => p
            .Add(c => c.State, state)
            .Add(c => c.TotalDays, 30));

        Assert.Contains("London", cut.Find(".status-city").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Day 1 / 30", cut.Find(".status-day").TextContent);
        Assert.Contains("£2,500", cut.Find(".status-cash").TextContent);
        Assert.Contains("£5,000", cut.Find(".status-debt").TextContent);
        Assert.Contains("0 / 100", cut.Find(".status-space").TextContent);
    }

    [Fact]
    public void MarketList_shows_goods_on_sale_and_held_goods()
    {
        var engine = TestGame.Engine();
        var state = TestGame.StateWithMarket(engine, new() { ["cigs"] = 20 });
        engine.Buy(state, "cigs", 1);
        state.Inventory["art"] = 1; // held but not traded here today
        state.Market.Remove("art");

        var cut = Render<MarketList>(p => p.Add(c => c.State, state));

        var rows = cut.FindAll(".market-row");
        Assert.Equal(["cigs", "art"], rows.Select(r => r.GetAttribute("data-good")));
        Assert.Contains("Not traded here today", rows[1].TextContent);
    }

    [Fact]
    public void MarketList_raises_trade_event_when_buy_clicked()
    {
        var state = TestGame.StateWithMarket(TestGame.Engine(), new() { ["cigs"] = 20 });
        (Good Good, TradeMode Mode)? traded = null;

        var cut = Render<MarketList>(p => p
            .Add(c => c.State, state)
            .Add(c => c.OnTrade, args => traded = args));
        cut.FindAll("button").First(b => b.TextContent.Contains("Buy")).Click();

        Assert.Equal("cigs", traded?.Good.Id);
        Assert.Equal(TradeMode.Buy, traded?.Mode);
    }

    [Fact]
    public void MarketList_flags_goods_with_an_active_event()
    {
        var state = TestGame.StateWithMarket(TestGame.Engine(), new() { ["cigs"] = 20, ["phones"] = 900 });
        state.Events.Add(new MarketEvent("london", "phones", MarketEventKind.Spike, 3, 1, 5));

        var cut = Render<MarketList>(p => p.Add(c => c.State, state));

        Assert.Contains("SPIKE", cut.Find("[data-good=phones]").TextContent);
        Assert.DoesNotContain("SPIKE", cut.Find("[data-good=cigs]").TextContent);
    }

    [Fact]
    public void NewsFeed_shows_headlines_with_status()
    {
        NewsItem[] news =
        [
            new() { Day = 3, CityId = "dubai", GoodId = "phones", Kind = MarketEventKind.Spike, Headline = "Phones soaring", Status = NewsStatus.Rumour },
            new() { Day = 2, CityId = "london", GoodId = "art", Kind = MarketEventKind.Crash, Headline = "Art crashing", Status = NewsStatus.False },
        ];

        var cut = Render<NewsFeed>(p => p.Add(c => c.Items, news));

        var items = cut.FindAll(".news-item");
        Assert.Equal(2, items.Count);
        Assert.Contains("UNVERIFIED", items[0].TextContent);
        Assert.Contains("DUBAI", items[0].TextContent);
        Assert.Contains("▲ SPIKE", items[0].TextContent);
        Assert.Contains("status-false", items[1].ClassName);
        Assert.Contains("FALSE", items[1].TextContent);
    }

    [Fact]
    public void NewsFeed_shows_empty_message()
    {
        var cut = Render<NewsFeed>(p => p.Add(c => c.Items, Array.Empty<NewsItem>()));

        Assert.NotNull(cut.Find(".news-empty"));
    }

    [Fact]
    public void HighScoreTable_shows_empty_message()
    {
        var cut = Render<HighScoreTable>(p => p.Add(c => c.Scores, Array.Empty<HighScore>()));

        Assert.NotNull(cut.Find(".score-empty"));
    }

    [Fact]
    public void HighScoreTable_lists_scores_in_order()
    {
        HighScore[] scores =
        [
            new(Guid.NewGuid(), 50_000, DateTime.UtcNow, "Dubai"),
            new(Guid.NewGuid(), -1_000, DateTime.UtcNow, "London"),
        ];

        var cut = Render<HighScoreTable>(p => p.Add(c => c.Scores, scores));

        var rows = cut.FindAll(".score-row");
        Assert.Equal(2, rows.Count);
        Assert.Contains("£50,000", rows[0].TextContent);
        Assert.Contains("Dubai", rows[0].TextContent);
        Assert.Contains("-£1,000", rows[1].TextContent);
    }

    [Fact]
    public void HighScoreTable_pages_and_highlights_the_latest_run()
    {
        var now = DateTime.UtcNow;
        var scores = Enumerable.Range(0, 25)
            .Select(i => new HighScore(Guid.NewGuid(), 100_000 - i * 1_000, now.AddMinutes(-i), "London", EndReason.Finished, now.AddMinutes(-i - 30), 30, 500))
            .ToList();
        var latest = scores[0].RunId;

        var cut = Render<HighScoreTable>(p => p.Add(c => c.Scores, scores));

        Assert.Equal(20, cut.FindAll(".score-row").Count);
        Assert.Contains("LATEST", cut.Find(".score-me").TextContent);
        Assert.Contains("best trade £500", cut.Find(".score-me").TextContent);
        cut.FindAll("button").Single(b => b.TextContent.Contains("Show more")).Click();
        Assert.Equal(25, cut.FindAll(".score-row").Count);
    }

    [Theory]
    [InlineData(EndReason.Killed, "KILLED")]
    [InlineData(EndReason.Retired, "RETIRED")]
    [InlineData(EndReason.Finished, "FINISHED")]
    public void HighScoreTable_shows_how_the_run_ended(EndReason reason, string label) =>
        Assert.Contains(label, HighScoreTable.OutcomeLabel(reason));

    [Fact]
    public void HighScoreTable_formats_run_dates()
    {
        var start = new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc);
        var sameDay = new HighScore(Guid.NewGuid(), 1, start.AddMinutes(45), "London", StartedUtc: start);
        var noStart = new HighScore(Guid.NewGuid(), 1, start, "London");

        Assert.Contains("–", HighScoreTable.Dates(sameDay));
        Assert.StartsWith("Ended", HighScoreTable.Dates(noStart));
    }
}

public class NeonComponentTests : BunitContext
{
    [Theory]
    [InlineData(BlackMarketEmpire.Shared.NeonIconKind.Credits)]
    [InlineData(BlackMarketEmpire.Shared.NeonIconKind.Heat)]
    [InlineData(BlackMarketEmpire.Shared.NeonIconKind.Market)]
    public void NeonIcon_renders_svg_and_heat_uses_magenta_class(BlackMarketEmpire.Shared.NeonIconKind kind)
    {
        var cut = Render<BlackMarketEmpire.Shared.NeonIcon>(p => p.Add(c => c.Kind, kind));

        Assert.NotEmpty(cut.Find("svg").Children);
        Assert.Equal(kind == BlackMarketEmpire.Shared.NeonIconKind.Heat, cut.Find(".neon-icon").ClassList.Contains("heat"));
    }

    [Fact]
    public void Wordmark_spells_out_the_name()
    {
        var cut = Render<BlackMarketEmpire.Shared.Wordmark>();

        Assert.Equal("BLACK_MARKET", cut.Find(".wm-top").TextContent);
        Assert.Equal("EMPIRE", cut.Find(".wm-bottom").TextContent);
    }
}

public class EncounterComponentTests : BunitContext
{
    public EncounterComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void RapSheet_shows_stats_and_reality_check()
    {
        var stats = new RunStats { Stops = 3, Arrests = 1, Escapes = 2, BribesPaid = 500 };

        var cut = Render<RapSheet>(p => p.Add(c => c.Stats, stats).Add(c => c.Reason, EndReason.Finished));

        Assert.Contains("£500", cut.Markup);
        Assert.Contains("arrested 1 time.", cut.Find(".reality-check").TextContent);
    }

    [Theory]
    [InlineData(EndReason.Killed, 0, 0, "no restart")]
    [InlineData(EndReason.Finished, 2, 0, "odds catch up")]
    [InlineData(EndReason.Retired, 0, 0, "That was luck")]
    public void Reality_check_matches_how_the_run_went(EndReason reason, int stops, int arrests, string expected)
    {
        var text = RapSheet.RealityCheck(reason, new RunStats { Stops = stops, Arrests = arrests });

        Assert.Contains(expected, text);
    }

    [Fact]
    public void StatusPanel_shows_health()
    {
        var state = TestGame.Engine().NewGame();
        state.Health = 30;

        var cut = Render<StatusPanel>(p => p.Add(c => c.State, state).Add(c => c.TotalDays, 30));

        Assert.Equal("30", cut.Find(".status-health").TextContent);
        Assert.Contains("negative", cut.Find(".status-health").ClassName);
    }
}

public class LogisticsComponentTests : BunitContext
{
    public LogisticsComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void DeliveriesPanel_hidden_with_no_shipments()
    {
        var cut = Render<DeliveriesPanel>(p => p.Add(c => c.State, new GameState { CityId = "london" }));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void DeliveriesPanel_offers_collect_when_goods_are_here()
    {
        var state = new GameState { CityId = "london" };
        state.Shipments.Add(new Shipment { GoodId = "cigs", Quantity = 7, ToCityId = "london", Status = ShipmentStatus.Delivered });
        state.Shipments.Add(new Shipment { GoodId = "art", Quantity = 1, ToCityId = "dubai", Status = ShipmentStatus.InTransit, ArrivalDay = 9 });
        var collected = false;

        var cut = Render<DeliveriesPanel>(p => p.Add(c => c.State, state).Add(c => c.OnCollect, () => collected = true));
        cut.FindAll("button").Single(b => b.TextContent.Contains("Collect 7")).Click();

        Assert.True(collected);
        Assert.Contains("due day 9", cut.Markup);
    }

    [Fact]
    public void StatusPanel_shows_heat_for_current_city()
    {
        var state = TestGame.Engine().NewGame();
        state.Heat["london"] = 60;
        state.Heat["dubai"] = 10;

        var cut = Render<StatusPanel>(p => p.Add(c => c.State, state).Add(c => c.TotalDays, 30));

        Assert.Equal("60", cut.Find(".status-heat").TextContent);
    }
}

public class FinanceComponentTests : BunitContext
{
    public FinanceComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void StatusPanel_shows_total_owed_bank_and_overdue()
    {
        var state = TestGame.Engine().NewGame();
        state.Bank = 1_200;
        state.Day = 10;
        state.Loans.Add(new Loan { LenderId = "eddie", Owed = 2_000, DueDay = 3 });

        var cut = Render<StatusPanel>(p => p.Add(c => c.State, state).Add(c => c.TotalDays, 30));

        Assert.Contains("£7,000", cut.Find(".status-debt").TextContent);
        Assert.Contains("£1,200", cut.Find(".status-bank").TextContent);
        Assert.NotNull(cut.Find(".status-overdue"));
        Assert.Contains("Medium danger", cut.Markup);
    }
}
