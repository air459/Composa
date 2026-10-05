using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Composa.App.Dialogs;
using Composa.Editing;
using Composa.Filters;
using Composa.IO;
using SkiaSharp;

namespace Composa.App.Tests;

public class ChineseLocalizationTests
{
    private sealed class ChineseScope : IDisposable
    {
        private readonly bool previous = L10n.Enabled;
        public ChineseScope() => L10n.Enabled = true;
        public void Dispose() => L10n.Enabled = previous;
    }

    [AvaloniaFact]
    public void Camera_raw_uses_the_lighting_context_without_changing_color_labels()
    {
        using var language = new ChineseScope();
        var window = new MainWindow { Width = 1280, Height = 800 };
        window.Show();
        try
        {
            var session = EditorSession.NewCanvas(16, 16, SKColors.White);
            window.AddSession(session);
            _ = CameraRawDialog.Show(window, new CameraRawSettings(), session.ActiveLayer!.Pixels!, _ => { }, () => session.ActiveLayer?.Pixels);
            Dispatcher.UIThread.RunJobs();
            var dialog = Assert.Single(window.OwnedWindows);
            var groups = dialog.GetVisualDescendants().OfType<Expander>().ToArray();
            Assert.Equal(9, groups.Length);
            Assert.Equal("光线", ((Grid)groups[0].Header!).Children.OfType<TextBlock>().Single().Text);
            Assert.Equal("亮色", Ui.Label("Light").Text);
            dialog.Close();
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Chinese_menu_executes_the_original_command_and_preserves_history_identifiers()
    {
        using var language = new ChineseScope();
        var window = new MainWindow { Width = 1280, Height = 800 };
        window.Show();
        try
        {
            var session = EditorSession.NewCanvas(32, 32, SKColors.White);
            window.AddSession(session);
            Dispatcher.UIThread.RunJobs();
            var add = window.GetLogicalDescendants().OfType<MenuItem>()
                .Single(item => item.Header as string == L10n.T("New Layer"));
            var count = session.Document.Layers.Count;
            add.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(count + 1, session.Document.Layers.Count);
            Assert.Equal("New Layer", session.History.UndoName);
            Assert.Equal("撤销 新建图层", L10n.T("Undo " + session.History.UndoName));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Dynamic_notices_preserve_user_filenames_and_translate_only_the_operation()
    {
        using var language = new ChineseScope();
        Assert.Equal("已保存“Normal: 我的照片.cube”，未包含效果", L10n.T("Saved Normal: 我的照片.cube without Effects"));
        Assert.Equal("Normal", Ui.UserLabel("Normal").Text);
        Assert.Equal("Background", Ui.UserLabel("Background").Text);
        Assert.Equal("正片叠底", L10n.T("Multiply"));
    }

    [AvaloniaFact]
    public void User_layer_names_survive_chinese_document_save_and_reopen()
    {
        using var language = new ChineseScope();
        var session = EditorSession.NewCanvas(16, 16, SKColors.Transparent);
        using var pixels = new SKBitmap(16, 16);
        pixels.Erase(SKColors.Red);
        session.AddImageLayer("Normal", pixels.Copy());
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cmps");
        try
        {
            ProjectFile.Save(session.Document, path);
            var reopened = ProjectFile.Load(path);
            Assert.Contains(reopened.Layers, layer => layer.Name == "Normal");
            Assert.Contains(reopened.Layers, layer => layer.Name == "背景");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
