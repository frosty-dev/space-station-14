using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.AirlockPainter.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class AirlockPainterWindow : DefaultWindow
    {
        public event Action<int>? OnSpritePicked;

        public AirlockPainterWindow()
        {
            RobustXamlLoader.Load(this);

            SpriteList.OnItemSelected += e => OnSpritePicked?.Invoke(e.ItemIndex);
        }

        public void Populate(List<AirlockPainterEntry> entries)
        {
            SpriteList.Clear();
            foreach (var entry in entries)
            {
                SpriteList.AddItem(entry.Name, entry.Icon);
            }
        }
    }
}
