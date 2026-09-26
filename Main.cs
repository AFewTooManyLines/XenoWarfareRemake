using System;
using System.Drawing.Text;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows.Forms.Design;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using SharpDX.Direct3D9;
using System.Net;
using System.Threading;
using System.IO;
using System.Drawing.Design;
using SharpDX.XAudio2;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using System.DirectoryServices.ActiveDirectory;
using System.Transactions;
using System.Diagnostics.Tracing;

namespace XenoWarfareRemake;

public class Main : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // defaults
    private static int width = 1000;
    private static float reference_width = 1500f;
    private static int height = 500;
    private static float reference_height = 1000f;

    private static int ground_level = 120;
    private static int gravity = Convert.ToInt32(0.6);

    // game states
    public static string game_state = "start_up";
    public static string menu_state = "home_menu";

    // menu and music handling variables
    public static int menu_option_number = 0;
    public static int menu_option_max = 0;
    public static string selected_menu;
    public static int selected_map_number;

    // key states
    public static KeyboardState previous_keyboard_state;

    // objects and functions
    class Player
    {
        // coords
        public Vector2 coords = new Vector2(0, 0);

        public int speed = 10;
        public int velocity_y = 0;
        public int jump_power = -20;
        public bool on_ground = true;

        // animation
        public int anim_value = 0;
        public Texture2D[] anim_left_list = new Texture2D[] { };
        public Texture2D[] anim_right_list = new Texture2D[] { };

        public Texture2D running_frame;

        // player information
        public int score = 0;

        // scaling
        public float scale = 0.2f;

        // direction
        public string direction = "none";
        public string last_direction = "right";

        // control keys
        public Keys right_key = Keys.D;
        public Keys left_key = Keys.A;
        public Keys jump_key = Keys.W;

        public void Update()
        {
            // some of the stupidest logic in the entire code is right here in this function

            if (anim_value >= anim_right_list.Length)
            {
                anim_value = 0;
            }

            if (direction == "none")
            {
                anim_value = 0;
            }

            // gravity
            velocity_y += gravity;
            coords.Y += velocity_y;

            // constraints
            if (coords.Y >= height - ground_level)
            {
                coords.Y = height - ground_level;
                velocity_y = 0;
                on_ground = true;
            }

            // handling animation
            if (direction == "left")
            {
                running_frame = anim_left_list[anim_value];
            }

            if (direction == "right")
            {
                running_frame = anim_right_list[anim_value];
            }

            if (direction == "none")
            {
                if (last_direction == "left")
                {
                    running_frame = anim_left_list[0];
                }

                else if (last_direction == "right")
                {
                    running_frame = anim_right_list[0];
                }
            }

            // motion
            if (Keyboard.GetState().IsKeyDown(right_key))
            {
                coords.X = coords.X + speed;
                direction = "right";
            }

            if (Keyboard.GetState().IsKeyDown(left_key))
            {
                coords.X = coords.X - speed;
                direction = "left";
            }

            if (Keyboard.GetState().IsKeyDown(jump_key) && on_ground)
            {
                velocity_y = jump_power;
                on_ground = false;
            }

            // syncing animation with motion
            if (Keyboard.GetState().IsKeyUp(right_key) && previous_keyboard_state.IsKeyDown(right_key) || Keyboard.GetState().IsKeyUp(left_key) && previous_keyboard_state.IsKeyDown(left_key))
            {
                last_direction = direction;
                direction = "none";
            }

            anim_value++;
        }
    }

    class Assets
    {
        public static Texture2D[] backgrounds = new Texture2D[] { };
        public static SpriteFont[] game_fonts = new SpriteFont[] { };
        public static Texture2D[] guns = new Texture2D[] { };
        public static Texture2D[] bullets = new Texture2D[] { };
        public static SoundEffect[] sound_effects = new SoundEffect[] { };
        public static Song[] music = new Song[] { };
    }

    class game_settings
    {
        public static string game_mode = "VERSUS";
        public static string map_selection_mode = "MANUAL";
    }

    class Menu
    {
        public static string selected_option = "";

        class options_list
        {
            // common
            public static string[] exit()
            {
                return new string[] { "exit", "home_menu" };
            }

            public static string[] go_back()
            {
                menu_option_number = 0;
                return new string[] { "begin", "home_menu" };
            }

            // home menu
            public static string[] play()
            {
                menu_option_number = 0;
                return new string[] { "begin", "map_selection_menu" };
            }

            public static string[] settings()
            {
                menu_option_number = 0;
                return new string[] { "begin", "settings" };
            }

            public static string[] credits()
            {
                menu_option_number = 0;
                return new string[] { "begin", "credits" };
            }

            // map selection menu
            public static string[] EARTH()
            {
                menu_option_number = 0;
                selected_map_number = 1;
                return new string[] { "transition", "home_menu" };
            }

            public static string[] ALIEN_PLANET()
            {
                menu_option_number = 0;
                selected_map_number = 2;
                return new string[] { "transition", "home_menu" };
            }

            public static string[] MARS()
            {
                menu_option_number = 0;
                selected_map_number = 3;
                return new string[] { "transition", "home_menu" };
            }

            // settings
            public static string[] switch_game_mode()
            {
                // I really can't be bothered to create a modular game mode switching system or something
                if (game_settings.game_mode == "VERSUS") game_settings.game_mode = "TOGETHER";
                else game_settings.game_mode = "VERSUS";

                return new string[] { "begin", "settings" };
            }

            public static string[] switch_map_selection_mode()
            {
                // same for this one
                if (game_settings.map_selection_mode == "MANUAL") game_settings.map_selection_mode = "RANDOM";
                else game_settings.map_selection_mode = "MANUAL";

                return new string[] { "begin", "settings" };
            }
        }

        public static Dictionary<string, Func<string[]>> option_functions = new Dictionary<string, Func<string[]>>
        {
            // common
            {"exit", options_list.exit},
            {"back", options_list.go_back},

            // home menu
            {"play", options_list.play},
            {"settings", options_list.settings},
            {"credits", options_list.credits},

            // map selection menu
            {"EARTH", options_list.EARTH},
            {"ALIEN_PLANET", options_list.ALIEN_PLANET},
            {"MARS", options_list.MARS},

            // settings
            {"game_mode", options_list.switch_game_mode},
            {"map_selection", options_list.switch_map_selection_mode},
        };

        // lists
        public static string[] menu_list = new string[]
        {
            "home_menu",
            "map_selection_menu",
            "settings",
            "credits",
            "paused_menu",
        };

        public static int[] menu_list_max_options = new int[]
        {
            3, // home
            3, // map selection
            2, // settings
            0, // credits
            2, // paused menu
        };

        internal static void create_option(SpriteBatch spriteBatch, string option_text, Vector2 position, Color colour)
        {
            spriteBatch.DrawString(Assets.game_fonts[1], option_text, position, colour);
        }

        internal static void render_colourful_text(SpriteBatch spriteBatch, string text, Vector2 pos)
        {
            Color[] colour_list = new Color[]
            {
              new Color(225, 75, 75),  // Red
              new Color(255, 165, 0),  // Orange
              new Color(255, 255, 0),  // Yellow
              new Color(0, 165, 0),    // Green
              new Color(0, 165, 165),  // Cyan
              new Color(197, 120, 227), // Purple
            };

            int letter_spacing = 15;

            for (int i = 0; i < text.Length; i++)
            {
                string char_ = Convert.ToString(text[i]);

                Color colour = colour_list[i % colour_list.Length];
                spriteBatch.DrawString(Assets.game_fonts[1], char_, pos, colour);

                pos.X += letter_spacing;
            }
        }

        // drawing functions
        public static void home_menu(SpriteBatch spriteBatch)
        {
            // title
            spriteBatch.DrawString(Assets.game_fonts[0], "Xeno Warfare", new Vector2(width / 2 - 200, height / 2 - 130), Color.Orange);

            // options
            void Draw(Color[] colour_list)
            {
                // options group together
                create_option(spriteBatch, "Play", new Vector2(width / 2 - 15, height / 2), colour_list[0]);
                create_option(spriteBatch, "Settings", new Vector2(width / 2 - 35, height / 2 + 40), colour_list[1]);
                create_option(spriteBatch, "Credits", new Vector2(width / 2 - 30, height / 2 + 80), colour_list[2]);

                // irregular y offset
                create_option(spriteBatch, "EXIT (E)", new Vector2(width / 2 - 35, height / 2 + 175), colour_list[3]);
            }

            Color[] selected_colours = new Color[] { };

            // switch case
            switch (menu_option_number)
            {
                case 0:
                    selected_colours = new Color[] { Color.Yellow, Color.White, Color.White, Color.Red, };
                    selected_option = "play";
                    break;

                case 1:
                    selected_colours = new Color[] { Color.White, Color.Yellow, Color.White, Color.Red, };
                    selected_option = "settings";
                    break;

                case 2:
                    selected_colours = new Color[] { Color.White, Color.White, Color.Yellow, Color.Red, };
                    selected_option = "credits";
                    break;

                case 3:
                    selected_colours = new Color[] { Color.White, Color.White, Color.White, Color.Yellow };
                    selected_option = "exit";
                    break;
            }

            // drawing
            Draw(selected_colours);
        }

        public static void map_selection_menu(SpriteBatch spriteBatch)
        {
            // title
            spriteBatch.DrawString(Assets.game_fonts[2], "CHOOSE", new Vector2(width / 2 - 55, height / 2 - 130), Color.HotPink);

            // options
            void Draw(Color[] colour_list)
            {
                // maps
                create_option(spriteBatch, "The Blue World", new Vector2(width / 2 - 83, height / 2 - 80), colour_list[0]);
                create_option(spriteBatch, "Home Sweet Home", new Vector2(width / 2 - 104, height / 2 - 40), colour_list[1]);
                create_option(spriteBatch, "Little Red Rock", new Vector2(width / 2 - 90, height / 2), colour_list[2]);

                // back option
                create_option(spriteBatch, "Back", new Vector2(width / 2 - 20, height / 2 + 175), colour_list[3]);
            }

            Color[] selected_colours = new Color[] { };

            // switch case
            switch (menu_option_number)
            {
                case 0:
                    selected_colours = new Color[] { Color.Yellow, Color.White, Color.White, Color.Red, };
                    selected_option = "EARTH";

                    // map info
                    if (menu_state == "map_selection_menu")
                    {
                        spriteBatch.DrawString(Assets.game_fonts[3], "An odd but beautiful planet bustling with life. No Contact", new Vector2(width / 2 - 290, height / 2 + 90), Color.Blue);
                    }

                    break;

                case 1:
                    selected_colours = new Color[] { Color.White, Color.Yellow, Color.White, Color.Red, };
                    selected_option = "ALIEN_PLANET";

                    // map info
                    spriteBatch.DrawString(Assets.game_fonts[3], "Chromara, our home. Rocky and cold. Beautiful rainbows light up the night sky.", new Vector2(width / 2 - 390, height / 2 + 90), Color.Yellow);

                    break;

                case 2:
                    selected_colours = new Color[] { Color.White, Color.White, Color.Yellow, Color.Red, };
                    selected_option = "MARS";

                    // map info
                    spriteBatch.DrawString(Assets.game_fonts[3], "A small planet covered in iron oxide, giving it a distinct red colour.", new Vector2(width / 2 - 317, height / 2 + 90), Color.Red);

                    break;

                case 3:
                    selected_colours = new Color[] { Color.White, Color.White, Color.White, Color.Yellow, };
                    selected_option = "back";
                    break;
            }

            Draw(selected_colours);
        }

        public static void settings(SpriteBatch spriteBatch)
        {
            // title
            spriteBatch.DrawString(Assets.game_fonts[2], "SETTINGS", new Vector2(width / 2 - 75, height / 2 - 130), Color.HotPink);

            // options
            void Draw(Color[] colour_list)
            {
                // game mode option
                create_option(spriteBatch, "Game Mode", new Vector2(width / 2 - 190, height / 2 - 70), colour_list[0]);

                // map selection
                create_option(spriteBatch, "Arena Selection", new Vector2(width / 2 - 200, height / 2 + 10), colour_list[1]);

                // back option
                create_option(spriteBatch, "Back", new Vector2(width / 2 - 25, height / 2 + 110), colour_list[2]);
            }

            Color[] selected_colours = new Color[] { };

            // game mode info text. Can't be bothered to refactor it
            if (game_settings.game_mode == "VERSUS")
            {
                spriteBatch.DrawString(Assets.game_fonts[1], game_settings.game_mode, new Vector2(width / 2 + 60, height / 2 - 70), Color.Red);
                spriteBatch.DrawString(Assets.game_fonts[3], "(Fight against each other and see who comes out on top!)", new Vector2(width / 2 - 300, height / 2 - 35), Color.Gray);
            }

            else if (game_settings.game_mode == "TOGETHER")
            {
                spriteBatch.DrawString(Assets.game_fonts[1], game_settings.game_mode, new Vector2(width / 2 + 50, height / 2 - 70), Color.LightGreen);
                spriteBatch.DrawString(Assets.game_fonts[3], "(Team up to fight against a common threat. Greater strength in greater numbers)", new Vector2(width / 2 - 400, height / 2 - 35), Color.Gray);
            }

            // map selection info text. Same with this one
            if (game_settings.map_selection_mode == "MANUAL")
            {
                spriteBatch.DrawString(Assets.game_fonts[1], game_settings.map_selection_mode, new Vector2(width / 2 + 60, height / 2 + 10), new Color(0, 165, 255));
                spriteBatch.DrawString(Assets.game_fonts[3], "(Save the game some processing power and pick the map on your own.)", new Vector2(width / 2 - 350, height / 2 + 45), Color.Gray);
            }

            else if (game_settings.map_selection_mode == "RANDOM")
            {
                render_colourful_text(spriteBatch, game_settings.map_selection_mode, new Vector2(width / 2 + 60, height / 2 + 10));
                spriteBatch.DrawString(Assets.game_fonts[3], "(Well, why not? Every game needs one, right? It is what it is.)", new Vector2(width / 2 - 320, height / 2 + 45), Color.Gray);
            }

            // switch case
            switch (menu_option_number)
            {
                case 0:
                    selected_colours = new Color[] { Color.Yellow, Color.White, Color.White };
                    selected_option = "game_mode";
                    break;

                case 1:
                    selected_colours = new Color[] { Color.White, Color.Yellow, Color.White };
                    selected_option = "map_selection";
                    break;

                case 2:
                    selected_colours = new Color[] { Color.White, Color.White, Color.Yellow };
                    selected_option = "back";
                    break;
            }


            // drawing
            Draw(selected_colours);
        }

        public static void credits(SpriteBatch spriteBatch)
        {
            // title
            void Draw(Color[] colour_list)
            {
                // just this one option
                create_option(spriteBatch, "Back", new Vector2(width / 2 - 10, height / 2 + 115), colour_list[0]);
            }

            Color[] selected_colours = new Color[] { };

            switch (menu_option_number)
            {
                case 0:
                    selected_colours = new Color[] { Color.Yellow };
                    selected_option = "back";
                    break;
            }

            Draw(selected_colours);

            // credits and graphics

            // programming
            spriteBatch.DrawString(Assets.game_fonts[1], "Shaurya Chanchal", new Vector2(width / 2 - 245, height / 2 - 130), Color.Orange);
            spriteBatch.DrawString(Assets.game_fonts[3], "- Programming, game design, UI design", new Vector2(width / 2 - 245, height / 2 - 100), Color.White);

            // game design and art
            spriteBatch.DrawString(Assets.game_fonts[1], "Mohammad Khosravi", new Vector2(width / 2 - 245, height / 2 - 55), new Color(0, 165, 255));
            spriteBatch.DrawString(Assets.game_fonts[3], "- Game design, artwork, animations", new Vector2(width / 2 - 245, height / 2 - 25), Color.White);

            // OpenGameArt
            spriteBatch.DrawString(Assets.game_fonts[1], "OpenGameArt.org", new Vector2(width / 2 - 245, height / 2 + 20), Color.Lime);
            spriteBatch.DrawString(Assets.game_fonts[3], "- Music, sound effects, fonts", new Vector2(width / 2 - 245, height / 2 + 50), Color.White);

            // Spaceship graphic
            spriteBatch.Draw(Assets.backgrounds[4], new Vector2(width / 2 + 280, height / 2 - 200), null, Color.White, 45f, Vector2.Zero, 0.15f, SpriteEffects.None, 0f);
        }

        public static void paused_menu(SpriteBatch spriteBatch)
        {
            
        }
    }

    Player player1;
    Player player2;

    public Main()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
    }

    protected override void Initialize()
    {
        // window control
        _graphics.IsFullScreen = false;
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;

        _graphics.ApplyChanges();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // creating new player objects
        player1 = new Player();
        player2 = new Player();

        // Loading player animaton sprites

        // player1
        player1.anim_left_list = new Texture2D[]
        {
            Content.Load<Texture2D>("Assets/Player1_animations/p1left"),
            Content.Load<Texture2D>("Assets/Player1_animations/p1leftm1"),
            Content.Load<Texture2D>("Assets/Player1_animations/p1leftm2"),
        };

        player1.anim_right_list = new Texture2D[]
        {
            Content.Load<Texture2D>("Assets/Player1_animations/p1right"),
            Content.Load<Texture2D>("Assets/Player1_animations/p1rightm1"),
            Content.Load<Texture2D>("Assets/Player1_animations/p1rightm2"),
        };

        player1.coords = new Vector2(width / 2 - 200, height - ground_level);
        player1.last_direction = "left";

        // player2
        player2.anim_left_list = new Texture2D[]
        {
            Content.Load<Texture2D>("Assets/Player2_animations/p2left"),
            Content.Load<Texture2D>("Assets/Player2_animations/p2leftm1"),
            Content.Load<Texture2D>("Assets/Player2_animations/p2leftm2"),
        };

        player2.anim_right_list = new Texture2D[]
        {
            Content.Load<Texture2D>("Assets/Player2_animations/p2right"),
            Content.Load<Texture2D>("Assets/Player2_animations/p2rightm1"),
            Content.Load<Texture2D>("Assets/Player2_animations/p2rightm2"),
        };

        player2.coords = new Vector2(width / 2 - 300, height - ground_level);
        player2.last_direction = "right";

        player2.right_key = Keys.Right;
        player2.left_key = Keys.Left;
        player2.jump_key = Keys.Up;

        // Loading assets

        // art
        Assets.backgrounds = new Texture2D[]
        {
          Content.Load<Texture2D>("Assets/Images/background"),
          Content.Load<Texture2D>("Assets/Images/Earth"),
          Content.Load<Texture2D>("Assets/Images/Mars"),
          Content.Load<Texture2D>("Assets/Images/AlienPlanet"),
          Content.Load<Texture2D>("Assets/Images/spaceship"),
          Content.Load<Texture2D>("Assets/Images/controls"),
        };

        // fonts
        Assets.game_fonts = new SpriteFont[]
        {
          Content.Load<SpriteFont>("Fonts/AstronFont"),
          Content.Load<SpriteFont>("Fonts/ByteBounceFont"),
          Content.Load<SpriteFont>("Fonts/SubHeading"),
          Content.Load<SpriteFont>("Fonts/InfoFont"),
        };

        // sound effects and music
        Assets.sound_effects = new SoundEffect[]
        {
            Content.Load<SoundEffect>("Audio/Intro"),
            Content.Load<SoundEffect>("Audio/OptionSelect"),
            Content.Load<SoundEffect>("Audio/OptionConfirm"),
            Content.Load<SoundEffect>("Audio/Losing"),
            Content.Load<SoundEffect>("Audio/Winner"),
            Content.Load<SoundEffect>("Audio/shoot"),
        };

        Assets.music = new Song[]
        {
            Content.Load<Song>("Audio/Menu"),
            Content.Load<Song>("Audio/Mercury"),
            Content.Load<Song>("Audio/Mars"),
            Content.Load<Song>("Audio/AlienPlanet"),
        };
        
        MediaPlayer.IsRepeating = true;
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState current_keyboard_state = Keyboard.GetState();

        // game state handler
        if (game_state == "start_up")
        {
            Assets.sound_effects[0].Play();
            
            Thread.Sleep(2000);
            game_state = "begin";
        }

        else if (game_state == "begin")
        {
            if (MediaPlayer.State != MediaState.Playing) MediaPlayer.Play(Assets.music[0]);

            // selecting menu
            for (int i = 0; i < Menu.menu_list.Length; i++)
            {
                if (Menu.menu_list[i] == menu_state)
                {
                    selected_menu = Menu.menu_list[i];
                    menu_option_max = Menu.menu_list_max_options[i];
                }
            }

            // navigation
            if (current_keyboard_state.IsKeyUp(Keys.Up) && previous_keyboard_state.IsKeyDown(Keys.Up))
            {
                if (menu_option_number != 0) menu_option_number--; Assets.sound_effects[1].Play();
            }

            if (current_keyboard_state.IsKeyUp(Keys.Down) && previous_keyboard_state.IsKeyDown(Keys.Down))
            {
                if (menu_option_number != menu_option_max) menu_option_number++; Assets.sound_effects[1].Play();
            }

            // select option
            if (current_keyboard_state.IsKeyUp(Keys.Enter) && previous_keyboard_state.IsKeyDown(Keys.Enter))
            {
                Assets.sound_effects[2].Play();

                Func<string[]> option = Menu.option_functions[Menu.selected_option];
                string[] states = option();

                game_state = states[0];
                menu_state = states[1];
            }

            // exit
            if (current_keyboard_state.IsKeyUp(Keys.E) && previous_keyboard_state.IsKeyDown(Keys.E)) Exit();
        }

        else if (game_state == "transition")
        {
            // not smart, but it is what it is

            MediaPlayer.Stop();
            
            Thread.Sleep(3000);
            
            MediaPlayer.Play(Assets.music[selected_map_number]);
            
            Thread.Sleep(500);
            
            game_state = "start";
        }

        else if (game_state == "start")
        {
            // player update
            player1.Update();
            player2.Update();
        }

        else if (game_state == "end")
        {
            
        }

        else if (game_state == "exit") Exit();

        previous_keyboard_state = current_keyboard_state;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin();

        if (game_state == "begin")
        {
            // background
            _spriteBatch.Draw(Assets.backgrounds[0], Vector2.Zero, null, Color.White, 0f, Vector2.Zero, new Vector2(width / reference_width, height / reference_height), SpriteEffects.None, 0f);

            // menu
            MethodInfo menu = typeof(Menu).GetMethod(selected_menu, BindingFlags.Static | BindingFlags.Public);
            object[] args = new object[] { _spriteBatch, };

            menu?.Invoke(this, args);
        }

        else if (game_state == "transition")
        {
            // default colour
            GraphicsDevice.Clear(Color.Black);

            // background image
            _spriteBatch.Draw(Assets.backgrounds[5], Vector2.Zero, null, Color.White, 0f, Vector2.Zero, new Vector2(width / 1900f, height / reference_height), SpriteEffects.None, 0f);
        }

        else if (game_state == "start")
        {
            // default colour
            GraphicsDevice.Clear(Color.White);
            
            // map background
            _spriteBatch.Draw(Assets.backgrounds[selected_map_number], Vector2.Zero, null, Color.White, 0f, Vector2.Zero, new Vector2(width / 750f, height / 500f), SpriteEffects.None, 0f);

            // drawing players
            if (player1.running_frame != null && player2.running_frame != null)
            {
                _spriteBatch.Draw(player1.running_frame, player1.coords, null, Color.White, 0f, Vector2.Zero, player1.scale, SpriteEffects.None, 0f);
                _spriteBatch.Draw(player2.running_frame, player2.coords, null, Color.White, 0f, Vector2.Zero, player2.scale, SpriteEffects.None, 0f);
            }

            // player1 test
            _spriteBatch.DrawString(Assets.game_fonts[1], "direction: " + player1.direction, new Vector2(10, 0), Color.Blue);
            _spriteBatch.DrawString(Assets.game_fonts[1], "last direction: " + player1.last_direction, new Vector2(10, 45), Color.Blue);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
