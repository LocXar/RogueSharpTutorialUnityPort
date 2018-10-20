﻿using RogueSharp;
using RogueSharpTutorial.Controller;
using RogueSharpTutorial.Model.Interfaces;

namespace RogueSharpTutorial.Model
{
    public class Item : IItem, ITreasure, IDrawable, IInventory
    {

        public  Colors  Color           { get; set; }
        public  char    Symbol          { get; set; }
        public  int     X               { get; set; }
        public  int     Y               { get; set; }
        public  Actor   Owner           { get; set; }
        public  int     MaxStack        { get; set; }
        public  int     CurrentStackAmount { get; set; }
        public  string  Name            { get; protected set; }
        public  int     RemainingUses   { get; protected set; }

        protected Game  game;

        public Item(Game game)
        {
            Symbol      = '!';
            Color       = Colors.Yellow;
            this.game   = game;
        }

        public Item(Game game, Actor owner)
        {
            Symbol = '!';
            Color = Colors.Yellow;
            this.game = game;
            Owner = owner;
        }

        public string GetName()
        {
            return Name;
        }

        public bool Use()
        {
            return UseItem();
        }

        protected virtual bool UseItem()
        {
            return false;
        }

        public bool PickUp(Actor actor)
        {
            if (actor != null)
            {
                if (actor.AddToInventory(this))
                {
                    if (actor is Player)
                    {
                        game.MessageLog.Add($"You picked up {Name}.");
                    }
                    return true;
                }
                else
                {
                    if (actor is Player)
                    {
                        game.MessageLog.Add($"You can't pick up {Name}.");
                    }
                }
            }

            return false;
        }

        public void Draw(IMap map)
        {
            if (!map.IsExplored(X, Y))
            {
                return;
            }

            if (map.IsInFov(X, Y))
            {
                game.SetMapCell(X, Y, Color, Colors.FloorBackground, Symbol, map.GetCell(X, Y).IsExplored);
            }
            else
            {
                game.SetMapCell(X, Y, Colors.YellowGray, Colors.FloorBackground, Symbol, map.GetCell(X, Y).IsExplored);
            }
        }
    }
}
