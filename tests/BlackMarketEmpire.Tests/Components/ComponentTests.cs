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

        var rows = cut.FindAll("tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("£50,000", rows[0].TextContent);
        Assert.Contains("Dubai", rows[0].TextContent);
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
