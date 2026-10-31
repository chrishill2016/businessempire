// Synthesised sound effects and vibration. No audio files: everything is generated with Web Audio.
(() => {
    let ctx;
    const audio = () => {
        ctx ??= new (window.AudioContext || window.webkitAudioContext)();
        if (ctx.state === 'suspended') ctx.resume();
        return ctx;
    };

    // One tone: frequency (or [from, to] for a slide), start offset, length, wave, volume.
    function tone(freq, at = 0, len = 0.12, type = 'square', vol = 0.08) {
        const a = audio();
        const t = a.currentTime + at;
        const osc = a.createOscillator();
        const gain = a.createGain();
        osc.type = type;
        const [from, to] = Array.isArray(freq) ? freq : [freq, freq];
        osc.frequency.setValueAtTime(from, t);
        if (to !== from) osc.frequency.exponentialRampToValueAtTime(Math.max(to, 1), t + len);
        gain.gain.setValueAtTime(vol, t);
        gain.gain.exponentialRampToValueAtTime(0.0001, t + len);
        osc.connect(gain).connect(a.destination);
        osc.start(t);
        osc.stop(t + len + 0.02);
    }

    const sounds = {
        buy: () => { tone(660, 0, 0.07); tone(880, 0.06, 0.09); },
        sell: () => { tone(988, 0, 0.06, 'triangle', 0.1); tone(1319, 0.06, 0.16, 'triangle', 0.1); },
        money: () => { tone(784, 0, 0.08, 'triangle'); tone(1047, 0.08, 0.12, 'triangle'); },
        travel: () => tone([220, 660], 0, 0.35, 'sawtooth', 0.04),
        siren: () => { for (let i = 0; i < 3; i++) { tone(960, i * 0.36, 0.18, 'square', 0.05); tone(720, i * 0.36 + 0.18, 0.18, 'square', 0.05); } },
        escape: () => { tone(523, 0, 0.08); tone(659, 0.08, 0.08); tone(784, 0.16, 0.14); },
        arrest: () => { tone(196, 0, 0.25, 'sawtooth', 0.07); tone(147, 0.25, 0.45, 'sawtooth', 0.07); },
        hurt: () => tone([300, 80], 0, 0.25, 'sawtooth', 0.08),
        death: () => { tone(392, 0, 0.3, 'triangle', 0.1); tone(330, 0.3, 0.3, 'triangle', 0.1); tone(262, 0.6, 0.9, 'triangle', 0.1); },
        error: () => tone(140, 0, 0.15, 'square', 0.05),
        achievement: () => [523, 659, 784, 1047].forEach((f, i) => tone(f, i * 0.09, 0.18, 'triangle', 0.09)),
        tip: () => { tone(1175, 0, 0.06, 'sine', 0.08); tone(1568, 0.07, 0.1, 'sine', 0.08); },
        finish: () => [392, 523, 659, 784, 1047].forEach((f, i) => tone(f, i * 0.12, 0.25, 'triangle', 0.09)),
    };

    const vibrations = {
        buy: 15, sell: [15, 40, 15], money: 15, travel: 30,
        siren: [200, 100, 200, 100, 200], escape: [30, 30, 30], arrest: [400], hurt: [120, 60, 120],
        death: [600, 200, 600], error: 40, achievement: [40, 40, 40, 40, 120], tip: 20, finish: [60, 60, 200],
    };

    window.bmeSfx = {
        play(cue, sound, vibrate) {
            try { if (sound && sounds[cue]) sounds[cue](); } catch { /* audio not available */ }
            try { if (vibrate && navigator.vibrate && vibrations[cue]) navigator.vibrate(vibrations[cue]); } catch { /* no vibration */ }
        }
    };
})();
