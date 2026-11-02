using PlayEdu.Models;
namespace PlayEdu.Services;
public static class Simulation {
    public static int[] Metrics(ScenarioData data, IEnumerable<RunChoice> choices) {
        var values = data.Metrics.Select(m => m.Initial).ToArray();
        foreach (var choice in choices.OrderBy(c => c.StepNumber)) {
            if (choice.StepNumber < 1 || choice.StepNumber > data.Scenes.Count) continue;
            var selected = data.Scenes[choice.StepNumber - 1].Choices.FirstOrDefault(c => c.Key == choice.Choice);
            if (selected == null) continue;
            for (var i=0; i<values.Length; i++) values[i] = Math.Clamp(values[i]+selected.Deltas[i],0,100);
        }
        return values;
    }
    public static (decimal Decision, decimal Business) Score(ScenarioData data, IEnumerable<RunChoice> choices) {
        var selected = choices.Select(c => data.Scenes[c.StepNumber-1].Choices.Single(d => d.Key == c.Choice)).ToList();
        var decision = selected.Sum(c => c.Quality == "good" ? 1m : c.Quality == "mixed" ? .5m : 0m) / data.Scenes.Count * 60;
        var business = Metrics(data, choices).Average() / 100 * 20;
        return (Math.Round(decision,2), Math.Round((decimal)business,2));
    }
}

