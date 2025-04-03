using UnityEngine;

/// <summary>
/// Central repository for all game text to make localization and editing easier
/// </summary>
public static class GameText
{
    // Environment Names
    public static string GetEnvironmentName(int level)
    {
        switch (level)
        {
            case 0:
                return "Ocean";
            case 1:
                return "River Mouth";
            case 2:
                return "River Central";
            case 3:
                return "Spawning Grounds";
            default:
                return $"Level {level}";
        }
    }

    // Level Descriptions - shown at the start of each level
    public static readonly string[] LevelDescriptions = new string[]
    {
        "You are a sockeye salmon! After growing in freshwater for your first year of life, you made your way downstream to the ocean. Now at sea, you've spent a couple more years building up the strength to return home to spawn.\n\n" +
        "Collect points by swimming into schools of food (zooplankton and shrimp), and avoid getting caught by predators and fishing boats. Gathering more points will attract more fish to join your school, so try to get as many as you can before time runs out!",

        "After braving the dangers of the ocean, you and your school are among the millions of salmon that eventually begin the long journey home this year.\n\n" +
        "Make it past the river mouth by avoiding hungry Harbor Seals. They're quick and can sense movement in the water with their whiskers, but they need to return to the surface to breathe. If one chases you, stay away from the surface and swim as fast as you can. You'll gain points for each fish you lead safely to the end of the level.",

        "You and your school have been swimming for days, and your bodies have begun to change, adapting to the freshwater river. As you get closer to the stream where you where born, you also begin to show your spawning colours. The males of your kind have even grown humped backs and hooked jaws to show off their strength!\n\n" +
        "Survive bears as you continue upstream. Once they grab you, they'll start carrying you to land, making it hard to struggle free and get back to the water. Although they can plunge after you, they can't swim out into deeper water. Lead as many fish as you can upstream to score points.",

        "It's been over two weeks since you began your journey at the river mouth. Using your incredible sense of smell, you and your school have traced your way back to the stream where you were born.\n\n" +
        "Now you must safely reach the spawning grounds, where your kind can dig their nests and lay their eggs in the gravel. For each fish you lead to the spawning grounds, you'll recieve double the points!"
    };

    // Level Completion Texts - shown after completing each level
    public static readonly string[] LevelCompletionTexts = new string[]
    {
        "You made it past the river mouth! But the journey isn't over yet. From here, your school will have to swim upstream for hundreds of kilometers.",
        "You made it past the river mouth! But the journey isn't over yet. From here, your school will have to swim upstream for hundreds of kilometers.",
        "Congratulations on getting past another challenge! From now on, it'll only be a few more days of swimming until you reach home.",
        "After an exhausting journey, you've completed your lifecycle! Each female Sockeye salmon will lay as many as 4,000 eggs, which will hatch in the winter. In about four years, the next generation of sockeye salmon will repeat your journey. Along the way, your kind has helped feed all kinds of animals, making the entire ocean, river, and forest healthier.\n\n" +
        "However, because of global warming, future generations might have an even harder time than you. During the winter, if too much rain falls instead of snow, floods can push salmon eggs out of their nests, or flush young salmon into the ocean before they're ready. Global warming is also causing glaciers to melt earlier, so young salmon have less water while they spend summer in the river. In warmer waters, salmon also become tired sooner, making it harder to complete their journey.\n\n" +
        "To donate or learn more about salmon, visit the Pacific Salmon Foundation online."
    };

    // Scoring Descriptions - explains how scoring works for each level
    public static readonly string[] ScoringDescriptions = new string[]
    {
        "In the ocean phase, you earn 1 point for each fish that survives.",
        "In the ocean phase, you earn 1 point for each fish that survives.",
        "In the river phase, you earn 10 points for each fish that survives.",
        "In the upper river, you earn 10 points for each fish that survives.",
        "At the spawning grounds, you earn 20 points (double) for each fish that reaches the end!"
    };

    // Ocean Phase Text
    public static readonly string OceanIntroText =
        "You are a sockeye salmon! After growing in freshwater for your first year of life, you made your way downstream to the ocean. Now at sea, you've spent a couple more years building up the strength to return home to spawn.\n\n" +
        "Collect points by swimming into schools of food (zooplankton and shrimp), and avoid getting caught by predators and fishing boats. Gathering more points will attract more fish to join your school, so try to get as many as you can before time runs out!\n\n" +
        "Controls:\nSPACE - Swim\nA, D - Turn Left/Right";

    // Title Labels for different screens
    public static readonly string OceanCompleteTitle = "OCEAN PHASE COMPLETE!";
    public static readonly string LevelCompleteFormat = "LEVEL {0} COMPLETE!";

    // Score Labels
    public static readonly string OceanScoreFormat = "Ocean Score: {0} points";
    public static readonly string RiverScoreFormat = "River Score: {0} points";
    public static readonly string TotalScoreFormat = "Total Score: {0} points";

    // Controls help text
    public static readonly string RiverControlsText =
        "Controls:\nSPACE - Swim, Struggle\nW, S - Pitch Up/Down\nA, D - Turn Left/Right\nSHIFT - Sprint";
}
