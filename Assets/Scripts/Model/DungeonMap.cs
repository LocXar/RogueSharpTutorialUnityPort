﻿using System.Collections.Generic;
using System.Linq;
using RogueSharp;
using RogueSharpTutorial.Model.Interfaces;
using RogueSharpTutorial.Controller;

namespace RogueSharpTutorial.Model
{
    public class DungeonMap : Map
    {
        private Game                            game;
        private MapGenerator                    mapGenerator;

        public  List<Rectangle>                 Rooms;
        public  List<Door>                      Doors           { get; set; }
        public  Stairs                          StairsUp        { get; set; }
        public  Stairs                          StairsDown      { get; set; }
        public  List<TreasurePile>              TreasurePiles   { get; set; }

        private readonly List<Monster>          monsters;

        public  bool                            stairsBlocked   { get; private set; }

        public DungeonMap (Game game)
        {
            this.game       = game;

            game.SchedulingSystem.Clear();                                                  // When going down a level, clear the move schedule

            Rooms           = new List<Rectangle>();
            monsters        = new List<Monster>();
            TreasurePiles   = new List<TreasurePile>();
            Doors           = new List<Door>();
        }

        /// <summary>
        /// Called by MapGenerator after we generate a new map to add the player to the map.
        /// </summary>
        /// <param name="player"></param>
        public void AddPlayer(Player player)
        {
            game.Player = player;
            SetIsWalkable(player.X, player.Y, false);
            game.SchedulingSystem.Add(player);
        }

        /// <summary>
        /// Set all the monsters links to the dungeon map and field of view. Have to do it this way as the monsters are created 
        /// before the map is fully created and returned to the Game. And therefore field of view is initialized properly.
        /// </summary>
        public void SetMonsters()
        {
            foreach(Monster actor in monsters)
            {
                actor.SetMapAwareness();
                actor.SetBehavior();
            }
        }

        /// <summary>
        /// Add a monster to the map and add to list of Monsters.
        /// </summary>
        /// <param name="monster"></param>
        public void AddMonster(Monster monster)
        {
            monsters.Add(monster);
            
            SetIsWalkable(monster.X, monster.Y, false);                                         // After adding the monster to the map make sure to make the cell not walkable

            if(monster.IsBoss)
            {
                stairsBlocked = true;
            }

            game.SchedulingSystem.Add(monster);
        }

        /// <summary>
        /// Remove a monster and set cell as walkable.
        /// </summary>
        /// <param name="monster"></param>
        public void RemoveMonster(Monster monster)
        {
            if(monster.IsBoss)
            {
                stairsBlocked = false;
            }
            monsters.Remove(monster);            
            SetIsWalkable(monster.X, monster.Y, true);                                          // After removing the monster from the map, make sure the cell is walkable again
            game.SchedulingSystem.Remove(monster);
        }

        /// <summary>
        /// Get a monster or null at specific x and y position.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Monster GetMonsterAt(int x, int y)
        {
            return monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }

        /// <summary>
        /// Get who is the boss monster of the level if there is one.
        /// </summary>
        /// <returns></returns>
        public Monster WhoIsBoss()
        {
            foreach(Monster monster in monsters)
            {
                if (monster.IsBoss)
                {
                    return monster;
                }
            }

            return null;
        }

        /// <summary>
        /// Test to see if the Player is at a position and return the player if so.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Player GetPlayerAt(int x, int y)
        {
            if (game.Player.X == x && game.Player.Y == y)
            {
                return game.Player;
            }

            return null;
        }

        /// <summary>
        /// Return a new List of all Treasure Piles at a location.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public List<TreasurePile> GetAllTreasurePilesAt(int x, int y)
        {
            return TreasurePiles.Where(g => g.X == x && g.Y == y).ToList();
        }

        /// <summary>
        /// Return the door at the x,y position or null if one is not found.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Door GetDoor(int x, int y)
        {
            return Doors.SingleOrDefault(d => d.X == x && d.Y == y);
        }

        /// <summary>
        /// The actor opens the door located at the x,y position.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void OpenDoor(Actor actor, int x, int y)
        {
            Door door = GetDoor(x, y);

            if (door != null && !door.IsOpen)
            {
                door.IsOpen = true;
                var cell = GetCell(x, y);
                
                SetCellProperties(x, y, true, cell.IsWalkable, cell.IsExplored);                            // Once the door is opened it should be marked as transparent and no longer block field-of-view

                game.MessageLog.Add(actor.Name + " opened a door");
            }
        }

        /// <summary>
        /// Add gold to the map. Will add the gold as a treasure pile.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="amount"></param>
        public void AddGold(int x, int y, int amount)
        {
            if (amount > 0)
            {
                AddTreasure(x, y, new Gold(amount, game));
            }
        }

        /// <summary>
        /// Add a treasure to the Dungeon Map.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="treasure"></param>
        public void AddTreasure(int x, int y, ITreasure treasure)
        {
            if (treasure is Gold)
            {
                List<TreasurePile> treasures = GetAllTreasurePilesAt(x, y);

                foreach(TreasurePile t in treasures)
                {
                    if(t.Treasure is Gold)
                    {
                        ((Gold)t.Treasure).Amount += ((Gold)treasure).Amount;
                        return;
                    }
                }
            }

            TreasurePiles.Add(new TreasurePile(x, y, treasure));
        }

        /// <summary>
        /// Remove a treasure from the Dungeon Map.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="treasure"></param>
        public void RemoveTreasure(TreasurePile treasure)
        {
            TreasurePiles.Remove(treasure);
        }

        /// <summary>
        /// Attempt to pick up treasure piles as a specific location.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void PickUpTreasure(Actor actor, int x, int y)
        {
            List<TreasurePile> treasureAtLocation = TreasurePiles.Where(g => g.X == x && g.Y == y).ToList();

            foreach (TreasurePile treasurePile in treasureAtLocation)
            {
                if (treasurePile.Treasure is Gold)
                {
                    treasurePile.Treasure.PickUp(actor);
                    TreasurePiles.Remove(treasurePile);
                }
                else
                {
                    if (actor is Player)
                    {
                        continue;
                    }
                    else if (actor.CanGrabTreasure && treasurePile.Treasure.PickUp(actor))
                    {
                        TreasurePiles.Remove(treasurePile);
                    }
                }
            }
        }

        /// <summary>
        /// The Draw method will be called each time the map is updated.
        /// It will send all of the symbols/colors for each cell to the graphics view
        /// </summary>
        public void Draw()
        {
            foreach (Cell cell in GetAllCells())
            {
                SetConsoleSymbolForCell(cell);
            }

            foreach (Door door in Doors)
            {
                door.Draw(this);
            }

            StairsUp.Draw(this);
            StairsDown.Draw(this);

            foreach (TreasurePile treasurePile in TreasurePiles)
            {
                IDrawable drawableTreasure = treasurePile.Treasure as IDrawable;
                drawableTreasure?.Draw(this);
            }

            int i = -1;                                                                         // Keep an index so we know which position to draw monster stats at
                                                                                                // Start at -1 in case no monsters in view, then can clear the stat bars
            foreach (Monster monster in monsters)                                               // Iterate through each monster on the map and draw it after drawing the Cells
            {
                monster.Draw(this);

                if (IsInFov(monster.X, monster.Y))                                              // When the monster is in the field-of-view also draw their stats
                {
                    i++;

                    monster.DrawStats(i);                                                       // Pass in the index to DrawStats and increment it afterwards
                }
            }
            if (i == -1)
            {
                game.ClearMonsterStats();
            }
        }

        /// <summary>
        /// This method will be called any time we move the player to update field-of-view.
        /// </summary>
        public void UpdatePlayerFieldOfView(Player player)
        {
            ComputeFov(player.X, player.Y, player.AwarenessAdjusted, true);                     // Compute the field-of-view based on the player's location and awareness
            foreach (Cell cell in GetAllCells())                                                // Mark all cells in field-of-view as having been explored
            {
                if (IsInFov(cell.X, cell.Y))
                {
                    SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
                }
            }
        }

        /// <summary>
        /// Returns true when able to place the Actor on the cell or false otherwise.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool SetActorPosition(Actor actor, int x, int y)
        {
            if (GetCell(x, y).IsWalkable)                                                       // Only allow actor placement if the cell is walkable
            {
                PickUpTreasure(actor, x, y);                                                    // If the actor walks over treasure pile, attempt to pick it up
                SetIsWalkable(actor.X, actor.Y, true);                                          // The cell the actor was previously on is now walkable
                actor.X = x;                                                                    // Update the actor's position
                actor.Y = y;
                SetIsWalkable(actor.X, actor.Y, false);                                         // The new cell the actor is on is now not walkable

                OpenDoor(actor, x, y);

                if (actor is Player)                                                            // Don't forget to update the field of view if we just repositioned the player
                {
                    UpdatePlayerFieldOfView(actor as Player);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if player is stairs so if can move down a level.
        /// </summary>
        /// <returns></returns>
        public bool CanMoveDownToNextLevel()
        {
            Player player = game.Player;
            return StairsDown.X == player.X && StairsDown.Y == player.Y;
        }

        /// <summary>
        /// Returns a List of Points of all monsters in the map.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Point> GetMonsterLocations()
        {
            return monsters.Select(m => new Point { X = m.X, Y = m.Y });
        }

        /// <summary>
        /// Returns a List of Points of all monsters in the player's field of view.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Point> GetMonsterLocationsInFieldOfView()
        {
            return monsters.Where(monster => IsInFov(monster.X, monster.Y))
               .Select(m => new Point { X = m.X, Y = m.Y });
        }

        /// <summary>
        /// Grt random walkable location in any room on the map.
        /// </summary>
        /// <returns></returns>
        public Point GetRandomLocation()
        {
            int roomNumber = Game.Random.Next(0, Rooms.Count - 1);
            Rectangle randomRoom = Rooms[roomNumber];

            if (!DoesRoomHaveWalkableSpace(randomRoom))
            {
                GetRandomLocation();
            }

            return GetRandomLocationInRoom(randomRoom);
        }

        /// <summary>
        /// Look for any random location in a room. Doesn't have to be walkable.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public Point GetRandomLocationInRoom(Rectangle room)
        {
            int x = Game.Random.Next(1, room.Width - 2) + room.X;
            int y = Game.Random.Next(1, room.Height - 2) + room.Y;
            if (!IsWalkable(x, y))
            {
                GetRandomLocationInRoom(room);
            }
            return new Point(x, y);
        }

        /// <summary>
        /// Look for a random location in the room that is walkable.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public Point GetRandomWalkableLocationInRoom(Rectangle room)
        {
            if (DoesRoomHaveWalkableSpace(room))
            {
                for (int i = 0; i < 100; i++)
                {
                    int x = Game.Random.Next(1, room.Width - 2) + room.X;
                    int y = Game.Random.Next(1, room.Height - 2) + room.Y;
                    if (IsWalkable(x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }

            return Point.Zero;                                                                      // If we didn't find a walkable location in the room return Point.Zero
        }

        /// <summary>
        /// Iterate through each Cell in the room and return true if any are walkable.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        public bool DoesRoomHaveWalkableSpace(Rectangle room)
        {
            for (int x = 1; x <= room.Width - 2; x++)
            {
                for (int y = 1; y <= room.Height - 2; y++)
                {
                    if (IsWalkable(x + room.X, y + room.Y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// A helper method for setting the IsWalkable property on a Cell.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="isWalkable"></param>
        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            Cell cell = GetCell(x, y) as Cell;
            SetCellProperties(cell.X, cell.Y, cell.IsTransparent, isWalkable, cell.IsExplored);
        }

        /// <summary>
        /// Set basic symbols for the map.
        /// </summary>
        /// <param name="cell"></param>
        private void SetConsoleSymbolForCell(Cell cell)
        {
            if (IsInFov(cell.X, cell.Y))                                                        // When a cell is currently in the field-of-view it should be drawn with lighter colors
            {
                if (cell.IsWalkable)                                                            // Choose the symbol to draw based on if the cell is walkable or not '.' for floor and '#' for walls
                {
                    game.SetMapCell(cell.X, cell.Y, Colors.FloorFov, Colors.FloorBackgroundFov, '.', cell.IsExplored);
                }
                else
                {
                    game.SetMapCell(cell.X, cell.Y, Colors.WallFov, Colors.WallBackgroundFov, '#', cell.IsExplored);
                }
            }
            else                                                                                // When a cell is outside of the field of view draw it with darker colors
            {
                if (cell.IsWalkable)
                {
                    game.SetMapCell(cell.X, cell.Y, Colors.Floor, Colors.FloorBackground, '.', cell.IsExplored);
                }
                else
                {
                    game.SetMapCell(cell.X, cell.Y, Colors.Wall, Colors.WallBackground, '#', cell.IsExplored);
                }
            }
        }
    }
}