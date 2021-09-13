using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace gameproject
{
    public class Crosshair
    {
        public static GraphicsDevice gDevice;
        public Vector2 Centre = new Vector2();
        private Color col = Color.LightGreen;
        public Texture2D CrosshairHorizontal;
        public Vector2[] CrosshairPosition = new Vector2[4];
        public Texture2D CrosshairVertical;
        private readonly string gapId = "xhair_gap";

        private readonly string lenId = "xhair_length";
        private readonly string thiId = "xhair_thickness";

        public Crosshair()
        {
            Global.RegisterVariable(lenId, 5, Create);
            Global.RegisterVariable(thiId, 2, Create);
            Global.RegisterVariable(gapId, 3, Create);

            Create();
        }

        public int Length
        {
            get => (int) Global.ConsoleVars[lenId].val;
            set
            {
                Global.ConsoleVars[lenId].val = value;
                Create();
            }
        }

        public int Thickness
        {
            get => (int) Global.ConsoleVars[thiId].val;
            set
            {
                Global.ConsoleVars[thiId].val = value;
                Create();
            }
        }

        public int CentreGap
        {
            get => (int) Global.ConsoleVars[gapId].val;
            set
            {
                Global.ConsoleVars[gapId].val = value;
                Create();
            }
        }

        public Color Colour
        {
            get => col;
            set
            {
                col = value;
                Create();
            }
        } // crosshair will automatically update when the values are changed

        public void Create()
        {
            CrosshairVertical = new Texture2D(gDevice, Thickness, Length);
            CrosshairHorizontal = new Texture2D(gDevice, Length, Thickness);

            var data = new Color[Length * Thickness];
            for (var i = 0; i < data.Length; ++i) data[i] = Colour;
            CrosshairVertical.SetData(data);
            CrosshairHorizontal.SetData(data);
            CrosshairPosition[0] = new Vector2(Main.WindowCentre.X - Thickness / 2, Main.WindowCentre.Y + CentreGap);
            CrosshairPosition[1] =
                new Vector2(Main.WindowCentre.X - Thickness / 2, Main.WindowCentre.Y - Length - CentreGap);
            CrosshairPosition[2] = new Vector2(Main.WindowCentre.X + CentreGap, Main.WindowCentre.Y - Thickness / 2);
            CrosshairPosition[3] =
                new Vector2(Main.WindowCentre.X - Length - CentreGap, Main.WindowCentre.Y - Thickness / 2);
        }
    }
}