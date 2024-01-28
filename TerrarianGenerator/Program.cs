using static SDL2.SDL;
using static SDL2.SDL_image;
using System;
using Engine2D;
using static Engine2D.Engine;
using static Engine2D.Engine.EventHandler;
using WM;
using SDL2;
using System.Data;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Collections.Immutable;

namespace Program
{

  public class Tile : Scene.GameObject
  {
    public string Type { get; init; }
    private List<(string, int)> _rule = [];
    public Tile (
                  string type, string name, string scene, 
                  nint texture, SDL_Rect textureDst
                ) : base()
    {
      Type   = type;
      _name  = name;
      _scene = scene;
      _textures.Add(texture);
      _textureDst = textureDst;
    }
    public Tile (
                  string type, string name, string scene, 
                  nint texture, SDL_Rect textureDst,
                  List<(string, int)> rule
                ) : base()
    {
      Type   = type;
      _name  = name;
      _scene = scene;
      _textures.Add(texture);
      _textureDst = textureDst;
      _rule = rule;
    }

    public List<(string, int)> GetRule() { return _rule; }
    public void SetRule(List<(string, int)> rule) { _rule = rule; }

    public Tile Copy() { return (Tile) MemberwiseClone(); }
  }

  public class TileGenerator
  {
    private readonly int _columns;
    private readonly int _rows;

    private readonly Dictionary<string, Tile> _tileTypes = [];
    private List<Tile> _tiles = [];

    public TileGenerator() {}
    public TileGenerator(int columns, int rows)
    {
      _columns = columns;
      _rows = rows;
    }

    private string GetTileType(int tileID)
    {
      if (tileID != 0)
      {
        
        if (tileID < _columns)
        {
          return GetRandomType(_tiles[tileID - 1].GetRule());
        }
        else if (tileID % _columns == 0)
        {
          return GetRandomType(_tiles[tileID - _rows].GetRule());
        }
        else 
        {
          return GetRandomType(
            ComparePossibleTypes(_tiles[tileID - 1].GetRule(),
                                 _tiles[tileID - _rows].GetRule()
                                )
                              );
        }
      }
      return GetRandomType(_tileTypes.Keys.ToList());
    }

    private static string GetRandomType(List<string> types)
    {
      return types[new Random().Next(types.Count)];
    }
    private static string GetRandomType(List<(string, int)> types)
    {
      int maxPercent = 0;
      foreach (var type in types)
      {
        maxPercent += type.Item2;
      }
      double rand = new Random().Next(maxPercent + 1);
      double percent = 0;
      foreach (var type in types)
      {
        percent += type.Item2;
        if (rand <= percent)
        {
          return type.Item1;
        }
      }
      return "";
    }

    private static List<(string, int)> ComparePossibleTypes(
        List<(string, int)> types1,
        List<(string, int)> types2
      )
    {
      List<(string, int)> types = [];
      foreach (var type1 in types1)
      {
        foreach (var type2 in types2)
        {
          if(type1.Item1 == type2.Item1)
          {
            types.Add((type1.Item1, type1.Item2 + type2.Item2));
          }
        }
      }
      return types;
    }

    public List<Tile> Generate(string scene)
    {
      _tiles = [];
      for (int row = 0; row < _rows; row++)
      {
        for (int column = 0; column < _columns; column++)
        {
          string type = GetTileType(_columns*row + column);

          Tile tile = _tileTypes[type].Copy();
          tile.SetName($"Tile{row}|{column}");
          tile.SetSceneName(scene);
          tile.SetX(column * tile.GetW());
          tile.SetY(row    * tile.GetH());
          
          _tiles.Add(tile);
        }
      }
      return _tiles;
    }

    public void Reset() { _tiles = []; }


    public void AddTileType(string name, Tile tile) { _tileTypes[name] = tile; }
    
    public Tile? GetTileType(string name)
    {
      Tile? tile;
      _tileTypes.TryGetValue(name, out tile);
      return tile;
    }
  }

  public class Program
  {
    public static TileGenerator generator = new();
    public static void Setup()
    {
       SetupEngine(
        SDL_INIT_VIDEO,

        IMG_InitFlags.IMG_INIT_PNG |
        IMG_InitFlags.IMG_INIT_JPG |
        IMG_InitFlags.IMG_INIT_WEBP
      );

      CreateWindow(
        "TerrarianGenerator",
        SDL_WINDOWPOS_UNDEFINED, 
        SDL_WINDOWPOS_UNDEFINED, 
        1024,
        1024,
        SDL_WindowFlags.SDL_WINDOW_SHOWN |
        SDL_WindowFlags.SDL_WINDOW_RESIZABLE /*|
        SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP*/,

        SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
        SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC
      );
      CreateScene("Main", Engine.GetWindow(1));
    }

    public static void Main(string[] args)
    {
      Setup();

      Window? window = GetScenesWindow("Main");
      if (window != null)
      {
        LoadTextures(
          SDL_GetWindowID(window.GetWindowPtr()),
          "../images/", 
          [ 
            "grass.png",
            "water.png",
            "sand.png",
            "mountain.png"
          ]
        );


        int columns = window.GetWindowW() / 2;
        int rows    = window.GetWindowH() / 2;

        generator = new TileGenerator (columns, rows);

        List<string> types = ["grass", "water", "sand", "mountain"];
        foreach (var type in types)
        {
          generator.AddTileType(
            type,
            new Tile
            (
              type, "", "", 
              GetTexture(type), new SDL_Rect { x = 0, y = 0, w = 2, h = 2 }
            )
          );
        }
        generator.GetTileType("grass")?.SetRule(
                                                [
                                                  ( "grass",    60 ),
                                                  ( "sand",     20 ),
                                                  ( "mountain", 20 )
                                                ]
                                               );
        generator.GetTileType("water")?.SetRule(
                                                [
                                                  ( "water", 95 ),
                                                  ( "sand",  5 )
                                                ]
                                               );
        generator.GetTileType("sand")?.SetRule(
                                                [
                                                  ( "grass", 80 ),
                                                  ( "sand",  20 ),
                                                  ( "water", 40 )
                                                ]
                                              );
        generator.GetTileType("mountain")?.SetRule(
                                                    [
                                                      ( "mountain", 60 ),
                                                      ( "grass",    40 )
                                                    ]
                                                  );


        foreach (var tile in generator.Generate("Main"))
        {
          AddObjectToScene(tile.GetSceneName(), tile);
        }
      }

      SetHandleKeyboardInputFunc(HandleKeyboardInput);

      RunEngine();

    }

    private static void HandleKeyboardInput()
    {
      if (( GetKey((int)SDL_Scancode.SDL_SCANCODE_LCTRL) ||
            GetKey((int)SDL_Scancode.SDL_SCANCODE_RCTRL) ) &&
          GetKey((int)SDL_Scancode.SDL_SCANCODE_Q))
      {
        StopEngine();
      }

      if (GetKey((int)SDL_Scancode.SDL_SCANCODE_UP))
      {
        Scene? scene = GetScene("Main");
        if (scene != null)
        {
          ref SDL_Point camCoord = ref scene.GetCamCoord();
          camCoord.y -= 5;
        }
      }
      if (GetKey((int)SDL_Scancode.SDL_SCANCODE_LEFT))
      {
        Scene? scene = GetScene("Main");
        if (scene != null)
        {
          ref SDL_Point camCoord = ref scene.GetCamCoord();
          camCoord.x -= 5;
        }
      }
      if (GetKey((int)SDL_Scancode.SDL_SCANCODE_DOWN))
      {
        Scene? scene = GetScene("Main");
        if (scene != null)
        {
          ref SDL_Point camCoord = ref scene.GetCamCoord();
          camCoord.y += 5;
        }
      }
      if (GetKey((int)SDL_Scancode.SDL_SCANCODE_RIGHT))
      {
        Scene? scene = GetScene("Main");
        if (scene != null)
        {
          ref SDL_Point camCoord = ref scene.GetCamCoord();
          camCoord.x += 5;
        }
      }

      if (GetKey((int)SDL_Scancode.SDL_SCANCODE_G))
      {
        UnpressKey((int)SDL_Scancode.SDL_SCANCODE_G);
        Window? window = GetScenesWindow("Main");
        if (window != null)
        {
          foreach (var tile in generator.Generate("Main"))
          {
            AddObjectToScene(tile.GetSceneName(), tile);
          }
        }

      }

    }

  }
}