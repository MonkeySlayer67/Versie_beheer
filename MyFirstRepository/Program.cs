using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();
Application.Run(new MainForm());

class MainForm : Form
{
    private readonly ComboBox caseComboBox = new();
    private readonly NumericUpDown countSelector = new();
    private readonly Button openButton = new();
    private readonly Button open5Button = new();
    private readonly Button open10Button = new();
    private readonly Button resetButton = new();
    private readonly Panel wheelPanel = new();
    private readonly Label resultLabel = new();
    private readonly Label skinNameLabel = new();
    private readonly Label weaponLabel = new();
    private readonly Label rarityValueLabel = new();
    private readonly ListView inventoryView = new();
    private readonly Label statsLabel = new();
    private readonly Label legendLabel = new();

    private readonly List<Case> cases;
    private readonly Dictionary<Skin, int> inventory = new();
    private readonly OpenStats stats = new();
    private readonly Random random = new();

    private List<Rarity> wheelSegments = new();
    private int wheelPosition;
    private Rarity currentTargetRarity;
    private Skin? currentSkin = null;
    private bool isSpinning;

    public MainForm()
    {
        Text = "CS:GO Case Simulator";
        ClientSize = new Size(980, 640);
        MinimumSize = new Size(900, 620);
        BackColor = Color.FromArgb(22, 24, 31);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);

        cases = CreateCases();
        InitializeLayout();
        RefreshUi();
    }

    private void InitializeLayout()
    {
        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(12),
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topLeftPanel = new Panel { Dock = DockStyle.Fill };
        var topRightPanel = new Panel { Dock = DockStyle.Fill };
        var bottomRightPanel = new Panel { Dock = DockStyle.Fill };

        mainLayout.Controls.Add(topLeftPanel, 0, 0);
        mainLayout.Controls.Add(topRightPanel, 1, 0);
        mainLayout.Controls.Add(wheelPanel, 0, 1);
        mainLayout.Controls.Add(bottomRightPanel, 1, 1);
        Controls.Add(mainLayout);

        wheelPanel.Dock = DockStyle.Fill;
        wheelPanel.BackColor = Color.FromArgb(16, 18, 24);
        wheelPanel.Paint += WheelPanel_Paint;

        var titleLabel = new Label
        {
            Text = "CS:GO Case Simulator",
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.White,
            Location = new Point(8, 8)
        };
        topLeftPanel.Controls.Add(titleLabel);

        var descriptionLabel = new Label
        {
            Text = "Pick a case, open drops, and watch the wheel spin in real time.",
            Font = new Font("Segoe UI", 10f),
            AutoSize = true,
            ForeColor = Color.LightGray,
            Location = new Point(10, 44)
        };
        topLeftPanel.Controls.Add(descriptionLabel);

        var labelCase = new Label
        {
            Text = "Case:",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(10, 88),
            AutoSize = true
        };
        topLeftPanel.Controls.Add(labelCase);

        caseComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        caseComboBox.Width = 300;
        caseComboBox.Location = new Point(10, 110);
        caseComboBox.SelectedIndexChanged += (_, _) => RefreshUi();
        topLeftPanel.Controls.Add(caseComboBox);

        var labelCount = new Label
        {
            Text = "Count:",
            Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(330, 88),
            AutoSize = true
        };
        topLeftPanel.Controls.Add(labelCount);

        countSelector.Minimum = 1;
        countSelector.Maximum = 50;
        countSelector.Value = 1;
        countSelector.Width = 80;
        countSelector.Location = new Point(330, 110);
        topLeftPanel.Controls.Add(countSelector);

        openButton.Text = "Open";
        openButton.Size = new Size(110, 42);
        openButton.Location = new Point(10, 160);
        openButton.BackColor = Color.FromArgb(0, 120, 215);
        openButton.ForeColor = Color.White;
        openButton.FlatStyle = FlatStyle.Flat;
        openButton.Click += async (_, _) => await OpenButton_Click();
        topLeftPanel.Controls.Add(openButton);

        open5Button.Text = "Open 5";
        open5Button.Size = new Size(110, 42);
        open5Button.Location = new Point(130, 160);
        open5Button.BackColor = Color.FromArgb(0, 150, 136);
        open5Button.ForeColor = Color.White;
        open5Button.FlatStyle = FlatStyle.Flat;
        open5Button.Click += async (_, _) => await OpenButton_Click(5);
        topLeftPanel.Controls.Add(open5Button);

        open10Button.Text = "Open 10";
        open10Button.Size = new Size(110, 42);
        open10Button.Location = new Point(250, 160);
        open10Button.BackColor = Color.FromArgb(255, 138, 101);
        open10Button.ForeColor = Color.White;
        open10Button.FlatStyle = FlatStyle.Flat;
        open10Button.Click += async (_, _) => await OpenButton_Click(10);
        topLeftPanel.Controls.Add(open10Button);

        resetButton.Text = "Reset";
        resetButton.Size = new Size(110, 42);
        resetButton.Location = new Point(370, 160);
        resetButton.BackColor = Color.FromArgb(96, 125, 139);
        resetButton.ForeColor = Color.White;
        resetButton.FlatStyle = FlatStyle.Flat;
        resetButton.Click += ResetButton_Click;
        topLeftPanel.Controls.Add(resetButton);

        resultLabel.Text = "Result: Ready to open a case.";
        resultLabel.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        resultLabel.AutoSize = false;
        resultLabel.Size = new Size(460, 36);
        resultLabel.Location = new Point(10, 220);
        topLeftPanel.Controls.Add(resultLabel);

        skinNameLabel.Text = "Skin: -";
        skinNameLabel.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        skinNameLabel.AutoSize = false;
        skinNameLabel.Size = new Size(460, 24);
        skinNameLabel.Location = new Point(10, 262);
        topLeftPanel.Controls.Add(skinNameLabel);

        weaponLabel.Text = "Weapon: -";
        weaponLabel.Font = new Font("Segoe UI", 9.75f, FontStyle.Regular);
        weaponLabel.AutoSize = false;
        weaponLabel.Size = new Size(460, 22);
        weaponLabel.Location = new Point(10, 290);
        weaponLabel.ForeColor = Color.LightGray;
        topLeftPanel.Controls.Add(weaponLabel);

        rarityValueLabel.Text = "Latest drop details appear here.";
        rarityValueLabel.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
        rarityValueLabel.AutoSize = false;
        rarityValueLabel.Size = new Size(460, 22);
        rarityValueLabel.Location = new Point(10, 316);
        rarityValueLabel.ForeColor = Color.LightGray;
        topLeftPanel.Controls.Add(rarityValueLabel);

        legendLabel.Text = "Legend:\nC = Consumer  I = Industrial  M = Mil-Spec\nR = Restricted  CL = Classified  CV = Covert  ★ = Rare Item";
        legendLabel.Font = new Font("Segoe UI", 9f);
        legendLabel.AutoSize = false;
        legendLabel.Size = new Size(460, 60);
        legendLabel.Location = new Point(10, 341);
        legendLabel.ForeColor = Color.LightGray;
        topLeftPanel.Controls.Add(legendLabel);

        var statsGroup = new GroupBox
        {
            Text = "Stats",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Dock = DockStyle.Fill,
        };
        topRightPanel.Controls.Add(statsGroup);

        statsLabel.Dock = DockStyle.Fill;
        statsLabel.ForeColor = Color.White;
        statsLabel.Font = new Font("Segoe UI", 10f);
        statsLabel.TextAlign = ContentAlignment.TopLeft;
        statsGroup.Controls.Add(statsLabel);

        var inventoryGroup = new GroupBox
        {
            Text = "Inventory",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Dock = DockStyle.Fill,
        };
        bottomRightPanel.Controls.Add(inventoryGroup);

        inventoryView.Dock = DockStyle.Fill;
        inventoryView.View = View.Details;
        inventoryView.FullRowSelect = true;
        inventoryView.GridLines = true;
        inventoryView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        inventoryView.Columns.Add("Item", 220);
        inventoryView.Columns.Add("Rarity", 100);
        inventoryView.Columns.Add("Count", 60, HorizontalAlignment.Right);
        inventoryView.Columns.Add("Value", 80, HorizontalAlignment.Right);
        inventoryGroup.Controls.Add(inventoryView);

        for (var i = 0; i < cases.Count; i++)
        {
            caseComboBox.Items.Add(cases[i].Name);
        }

        caseComboBox.SelectedIndex = 0;
    }

    private async Task OpenButton_Click()
    {
        var count = (int)countSelector.Value;
        await OpenButton_Click(count);
    }

    private async Task OpenButton_Click(int count)
    {
        if (isSpinning)
            return;

        var selectedCase = cases[caseComboBox.SelectedIndex];
        isSpinning = true;
        currentSkin = null;
        openButton.Enabled = false;
        open5Button.Enabled = false;
        open10Button.Enabled = false;
        resetButton.Enabled = false;
        caseComboBox.Enabled = false;
        countSelector.Enabled = false;

        for (var i = 0; i < count; i++)
        {
            var skin = await SpinCaseWheelAsync(selectedCase);
            currentSkin = skin;
            AddSkinToInventory(skin);
            UpdateDropLabels(skin);
            await Task.Delay(250);
        }

        isSpinning = false;
        openButton.Enabled = true;
        open5Button.Enabled = true;
        open10Button.Enabled = true;
        resetButton.Enabled = true;
        caseComboBox.Enabled = true;
        countSelector.Enabled = true;
        RefreshUi();
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        inventory.Clear();
        stats.TotalCasesOpened = 0;
        stats.TotalValue = 0;
        stats.RarityCounts.Clear();
        resultLabel.Text = "Result: Ready to open a case.";
        RefreshUi();
    }

    private async Task<Skin> SpinCaseWheelAsync(Case selectedCase)
    {
        wheelSegments = BuildWheel(selectedCase);
        currentTargetRarity = selectedCase.PickRarity(random);
        var targetIndices = wheelSegments
            .Select((rarity, index) => (rarity, index))
            .Where(item => item.rarity == currentTargetRarity)
            .Select(item => item.index)
            .ToArray();

        var baseSpins = random.Next(6, 10);
        var targetIndex = targetIndices[random.Next(targetIndices.Length)];
        var totalSteps = baseSpins + ((targetIndex - wheelPosition + wheelSegments.Count) % wheelSegments.Count) + wheelSegments.Count;

        for (var step = 0; step <= totalSteps; step++)
        {
            wheelPosition = step % wheelSegments.Count;
            wheelPanel.Invalidate();
            await Task.Delay(7 + Math.Min(step, 15) * 2);
        }

        return selectedCase.Open(random, currentTargetRarity);
    }

    private void AddSkinToInventory(Skin skin)
    {
        inventory[skin] = inventory.GetValueOrDefault(skin) + 1;
        stats.TotalCasesOpened++;
        stats.TotalValue += skin.Value;
        stats.RarityCounts[skin.Rarity] = stats.RarityCounts.GetValueOrDefault(skin.Rarity) + 1;
    }

    private void RefreshUi()
    {
        UpdateStats();
        UpdateInventory();
        wheelPanel.Invalidate();
    }

    private void UpdateStats()
    {
        var lines = new List<string>
        {
            $"Total opened: {stats.TotalCasesOpened}",
            $"Total value: ${stats.TotalValue:F2}",
            string.Empty,
            "Rarity counts:",
        };

        foreach (var rarity in Enum.GetValues<Rarity>().Cast<Rarity>())
        {
            lines.Add($"  {rarity.GetDisplayName()}: {stats.RarityCounts.GetValueOrDefault(rarity)}");
        }

        statsLabel.Text = string.Join("\n", lines);
    }

    private void UpdateInventory()
    {
        inventoryView.BeginUpdate();
        inventoryView.Items.Clear();

        foreach (var item in inventory.OrderByDescending(item => item.Key.Rarity).ThenBy(item => item.Key.Name))
        {
            var listItem = new ListViewItem(item.Key.Name);
            listItem.SubItems.Add(item.Key.Rarity.GetDisplayName());
            listItem.SubItems.Add(item.Value.ToString());
            listItem.SubItems.Add($"${item.Key.Value:F2}");
            inventoryView.Items.Add(listItem);
        }

        inventoryView.EndUpdate();
    }

    private void UpdateDropLabels(Skin skin)
    {
        skinNameLabel.Text = $"Skin: {skin.Name}";
        weaponLabel.Text = $"Weapon: {skin.Weapon}";
        rarityValueLabel.Text = $"{skin.Rarity.GetDisplayName()} | ${skin.Value:F2}";
    }

    private void WheelPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(wheelPanel.BackColor);

        if (wheelSegments.Count == 0)
        {
            using var font = new Font("Segoe UI", 12f, FontStyle.Bold);
            using var brush = new SolidBrush(Color.LightGray);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("Open a case to spin the crate wheel", font, brush, wheelPanel.ClientRectangle, sf);
            return;
        }

        var visibleCount = Math.Min(11, wheelSegments.Count);
        var segmentWidth = Math.Max(80, (wheelPanel.ClientSize.Width - 80) / visibleCount);
        var segmentHeight = 90;
        var startX = (wheelPanel.ClientSize.Width - segmentWidth * visibleCount) / 2;
        var y = wheelPanel.ClientSize.Height / 2 - segmentHeight / 2;
        var centerIndex = visibleCount / 2;

        for (var i = 0; i < visibleCount; i++)
        {
            var index = (wheelPosition - centerIndex + i + wheelSegments.Count) % wheelSegments.Count;
            var rarity = wheelSegments[index];
            var rect = new Rectangle(startX + i * segmentWidth, y, segmentWidth - 6, segmentHeight);
            using var brush = new SolidBrush(GetRarityColor(rarity));
            g.FillRectangle(brush, rect);
            using var pen = new Pen(i == centerIndex ? Color.White : Color.DimGray, i == centerIndex ? 4 : 2);
            g.DrawRectangle(pen, rect);

            using var textFont = new Font("Segoe UI", i == centerIndex ? 10f : 8f, i == centerIndex ? FontStyle.Bold : FontStyle.Regular);
            using var textBrush = new SolidBrush(Color.Black);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(rarity.GetShortName(), textFont, textBrush, rect, sf);
        }

        var pointer = new PointF(wheelPanel.ClientSize.Width / 2f, y - 12f);
        var triangle = new[]
        {
            new PointF(pointer.X, y - 2f),
            new PointF(pointer.X - 16f, y + 16f),
            new PointF(pointer.X + 16f, y + 16f),
        };
        g.FillPolygon(Brushes.White, triangle);
        g.DrawPolygon(Pens.Black, triangle);

        var coverRect = new Rectangle(wheelPanel.ClientSize.Width / 2 - 170, y - 130, 340, 100);
        using (var brush = new SolidBrush(Color.FromArgb(30, 30, 45)))
        {
            FillRoundedRectangle(g, brush, coverRect, 12);
        }
        using (var pen = new Pen(currentSkin?.Rarity is Rarity rarity ? GetRarityColor(rarity) : Color.White, 3))
        {
            DrawRoundedRectangle(g, pen, coverRect, 12);
        }

        var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        var bodyFont = new Font("Segoe UI", 9f, FontStyle.Regular);
        var titleBrush = Brushes.White;
        var bodyBrush = Brushes.LightGray;
        var titleRect = new Rectangle(coverRect.Left + 16, coverRect.Top + 12, coverRect.Width - 32, 26);
        var bodyRect = new Rectangle(coverRect.Left + 16, coverRect.Top + 40, coverRect.Width - 32, 50);

        if (currentSkin != null)
        {
            g.DrawString(currentSkin.Name, titleFont, titleBrush, titleRect, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near });
            g.DrawString($"{currentSkin.Rarity.GetDisplayName()} · ${currentSkin.Value:F2}", bodyFont, bodyBrush, bodyRect, new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near });
        }
        else
        {
            g.DrawString("Crate Front Cover", titleFont, titleBrush, titleRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near });
            g.DrawString("Open a case to reveal the skin!", bodyFont, bodyBrush, bodyRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near });
        }

        var targetRect = new Rectangle(startX, y + segmentHeight + 20, segmentWidth * visibleCount, 30);
        g.DrawString($"Target: {currentTargetRarity.GetDisplayName()}", bodyFont, Brushes.White, targetRect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }

    private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectanglePath(bounds, radius);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectanglePath(bounds, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        // top-left arc
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        // top-right arc
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        // bottom-right arc
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        // bottom-left arc
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }

    private List<Case> CreateCases()
    {
        return new List<Case>
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
            new Case("Operation Case", new Dictionary<Rarity, double>
            {
                [Rarity.Consumer] = 65.00,
                [Rarity.Industrial] = 20.00,
                [Rarity.MilSpec] = 8.00,
                [Rarity.Restricted] = 4.50,
                [Rarity.Classified] = 1.40,
                [Rarity.Covert] = 0.08,
                [Rarity.RareSpecialItem] = 0.02,
            }, new List<Skin>
            {
                new Skin("USP-S | Torque", Rarity.Consumer, 0.22m),
                new Skin("P2000 | Fire Elemental", Rarity.Consumer, 0.18m),
                new Skin("MAC-10 | Neon Rider", Rarity.Industrial, 0.25m),
                new Skin("XM1014 | Tranquility", Rarity.Industrial, 0.22m),
                new Skin("AK-47 | Redline", Rarity.MilSpec, 2.60m),
                new Skin("M4A1-S | Cyrex", Rarity.Restricted, 12.00m),
                new Skin("AWP | Asiimov", Rarity.Restricted, 60.00m),
                new Skin("Desert Eagle | Blaze", Rarity.Classified, 130.00m),
                new Skin("M4A4 | Neo-Noir", Rarity.Classified, 35.00m),
                new Skin("AK-47 | Fuel Injector", Rarity.Covert, 160.00m),
                new Skin("AWP | Dragon Lore", Rarity.Covert, 1800.00m),
                new Skin("★ Karambit | Doppler", Rarity.RareSpecialItem, 220.00m),
            }),
        };
    }

    private static List<Rarity> BuildWheel(Case selectedCase)
    {
        var wheel = new List<Rarity>();
        foreach (var rarity in Enum.GetValues<Rarity>())
        {
            var chance = selectedCase.RarityChances.GetValueOrDefault(rarity);
            var count = Math.Max(1, (int)Math.Round(chance * 0.25));
            for (var i = 0; i < count; i++)
            {
                wheel.Add(rarity);
            }
        }
        return wheel;
    }

    private static Color GetRarityColor(Rarity rarity) => rarity switch
    {
        Rarity.Consumer => Color.Gray,
        Rarity.Industrial => Color.LimeGreen,
        Rarity.MilSpec => Color.Cyan,
        Rarity.Restricted => Color.Gold,
        Rarity.Classified => Color.MediumPurple,
        Rarity.Covert => Color.OrangeRed,
        Rarity.RareSpecialItem => Color.DarkGoldenrod,
        _ => Color.White,
    };
}

record Skin(string Name, Rarity Rarity, decimal Value)
{
    public string Weapon => Name.Contains(" | ") ? Name.Split(" | ")[0] : Name;
}

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
        var rarity = PickRarity(random);
        var choices = Skins.Where(s => s.Rarity == rarity).ToList();
        return choices[random.Next(choices.Count)];
    }

    public Skin Open(Random random, Rarity forcedRarity)
    {
        var choices = Skins.Where(s => s.Rarity == forcedRarity).ToList();
        return choices[random.Next(choices.Count)];
    }

    public Rarity PickRarity(Random random)
    {
        var value = random.NextDouble() * 100;
        return _cdf.First(item => value <= item.threshold).rarity;
    }

    private static List<(Rarity rarity, double threshold)> BuildCdf(Dictionary<Rarity, double> rarityChances)
    {
        var result = new List<(Rarity rarity, double threshold)>();
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

    public static string GetShortName(this Rarity rarity) => rarity switch
    {
        Rarity.Consumer => "C",
        Rarity.Industrial => "I",
        Rarity.MilSpec => "M",
        Rarity.Restricted => "R",
        Rarity.Classified => "CL",
        Rarity.Covert => "CV",
        Rarity.RareSpecialItem => "★",
        _ => rarity.ToString().Substring(0, Math.Min(2, rarity.ToString().Length)),
    };
}
