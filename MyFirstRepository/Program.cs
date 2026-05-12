using System.Collections.Generic;
using System.Globalization;

var random = new Random();
var inventory = new Dictionary<Skin, int>();
var stats = new OpenStats();

var cases = new List<Case>
{
    new Case("Phoenix Case", new Dictionary<Rarity, double>
    {
        [Rarity.Consumer] = 79.92,
        [Rarity.Industrial] = 15.98,
        [Rarity.MilSpec] = 3.20,
        [Rarity.Restricted] = 0.64,
        [Rarity.Classified] = 0.128,
        [Rarity.Covert] = 0.03,
        [Rarity.RareSpecialItem] = 0.002,
    }, new List<Skin>
    {
        new Skin("Glock-18 | Groundwater", Rarity.Consumer, 0.05m),
        new Skin("P250 | Boreal Forest", Rarity.Consumer, 0.08m),
        new Skin("Dual Berettas | Briar", Rarity.Industrial, 0.20m),
        new Skin("Five-SeveN | Copper Galaxy", Rarity.Industrial, 0.12m),
        new Skin("AK-47 | Blue Laminate", Rarity.MilSpec, 1.20m),
        new Skin("M4A4 | Tornado", Rarity.MilSpec, 0.75m),
        new Skin("AWP | Electric Hive", Rarity.Restricted, 4.50m),
        new Skin("Desert Eagle | Hypnotic", Rarity.Restricted, 8.00m),
        new Skin("M4A1-S | Guardian", Rarity.Classified, 15.50m),
        new Skin("AK-47 | Jaguar", Rarity.Classified, 14.00m),
        new Skin("AWP | Asiimov", Rarity.Covert, 60.00m),
        new Skin("M4A4 | Howl", Rarity.Covert, 1400.00m),
        new Skin("★ M9 Bayonet | Doppler", Rarity.RareSpecialItem, 180.00m),
    }),
};

Console.WriteLine("CS:GO Case Simulator");
Console.WriteLine("Use the menu to open cases, track your inventory, and see stats.");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1) Open a case");
    Console.WriteLine("2) Show inventory");
    Console.WriteLine("3) Show stats");
    Console.WriteLine("4) Reset simulator");
    Console.WriteLine("5) Exit");
    Console.Write("Choose an option: ");
    var option = Console.ReadLine()?.Trim();

    if (option == "1")
    {
        OpenCaseMenu(cases, inventory, stats, random);
    }
    else if (option == "2")
    {
        PrintInventory(inventory);
    }
    else if (option == "3")
    {
        PrintStats(stats);
    }
    else if (option == "4")
    {
        inventory.Clear();
        stats = new OpenStats();
        Console.WriteLine("Simulator reset.");
    }
    else if (option == "5")
    {
        break;
    }
    else
    {
        Console.WriteLine("Invalid option. Enter 1-5.");
    }
}

static void OpenCaseMenu(List<Case> cases, Dictionary<Skin, int> inventory, OpenStats stats, Random random)
{
    Console.WriteLine();
    for (var i = 0; i < cases.Count; i++)
    {
        Console.WriteLine($"{i + 1}) {cases[i].Name}");
    }
    Console.Write("Select a case: ");
    if (!int.TryParse(Console.ReadLine(), out var caseIndex) || caseIndex < 1 || caseIndex > cases.Count)
    {
        Console.WriteLine("Invalid case selection.");
        return;
    }

    var selectedCase = cases[caseIndex - 1];
    Console.Write("How many cases do you want to open? ");
    if (!int.TryParse(Console.ReadLine(), out var count) || count <= 0)
    {
        Console.WriteLine("Enter a positive number.");
        return;
    }

    Console.WriteLine();
    for (var i = 0; i < count; i++)
    {
        var skin = selectedCase.Open(random);
        inventory[skin] = inventory.GetValueOrDefault(skin) + 1;
        stats.TotalCasesOpened++;
        stats.TotalValue += skin.Value;
        stats.RarityCounts[skin.Rarity]++;

        Console.WriteLine($"Opened {selectedCase.Name}: {skin.Name} ({skin.Rarity.GetDisplayName()}) - ${skin.Value:F2}");
    }
}

static void PrintInventory(Dictionary<Skin, int> inventory)
{
    Console.WriteLine();
    if (inventory.Count == 0)
    {
        Console.WriteLine("Inventory is empty. Open some cases first.");
        return;
    }
    foreach (var item in inventory.OrderByDescending(item => item.Key.Rarity).ThenBy(item => item.Key.Name))
    {
        Console.WriteLine($"{item.Value}x {item.Key.Name} ({item.Key.Rarity.GetDisplayName()}) - ${item.Key.Value:F2}");
    }
}

static void PrintStats(OpenStats stats)
{
    Console.WriteLine();
    Console.WriteLine($"Total cases opened: {stats.TotalCasesOpened}");
    Console.WriteLine($"Total value of drops: ${stats.TotalValue:F2}");
    foreach (var rarity in Enum.GetValues<Rarity>().OrderBy(r => (int)r))
    {
        Console.WriteLine($"{rarity.GetDisplayName()}: {stats.RarityCounts.GetValueOrDefault(rarity)}");
    }
}

record Skin(string Name, Rarity Rarity, decimal Value);

class Case
{
    public string Name { get; }
    public Dictionary<Rarity, double> RarityChances { get; }
    public List<Skin> Skins { get; }
    private readonly List<(Rarity rarity, double threshold)> _cdf;

    public Case(string name, Dictionary<Rarity, double> rarityChances, List<Skin> skins)
    {
        Name = name;
        RarityChances = rarityChances;
        Skins = skins;
        _cdf = BuildCdf(rarityChances);
    }

    public Skin Open(Random random)
    {
        var value = random.NextDouble() * 100;
        var rarity = _cdf.First(item => value <= item.threshold).rarity;
        var choices = Skins.Where(s => s.Rarity == rarity).ToList();
        return choices[random.Next(choices.Count)];
    }

    private static List<(Rarity rarity, double threshold)> BuildCdf(Dictionary<Rarity, double> rarityChances)
    {
        var result = new List<(Rarity, double)>();
        var sum = 0.0;
        foreach (var rarity in Enum.GetValues<Rarity>())
        {
            sum += rarityChances.GetValueOrDefault(rarity);
            result.Add((rarity, sum));
        }
        return result;
    }
}

class OpenStats
{
    public int TotalCasesOpened { get; set; }
    public decimal TotalValue { get; set; }
    public Dictionary<Rarity, int> RarityCounts { get; } = new();
}

enum Rarity
{
    Consumer = 1,
    Industrial,
    MilSpec,
    Restricted,
    Classified,
    Covert,
    RareSpecialItem,
}

static class RarityExtensions
{
    public static string GetDisplayName(this Rarity rarity) => rarity switch
    {
        Rarity.Consumer => "Consumer Grade",
        Rarity.Industrial => "Industrial Grade",
        Rarity.MilSpec => "Mil-Spec Grade",
        Rarity.Restricted => "Restricted",
        Rarity.Classified => "Classified",
        Rarity.Covert => "Covert",
        Rarity.RareSpecialItem => "Rare Special Item",
        _ => rarity.ToString(),
    };
}
