﻿using System.Collections.Generic;
using RogueSharpTutorial.View;
using RogueSharpTutorial.Controller;
using RogueSharpTutorial.Model.Interfaces;
using RogueSharp;

namespace RogueSharpTutorial.Model
{
    public class Actor : IActor, IDrawable, IScheduleable
    {
        // IActor
        public string   Name                { get; set; }
        public int      Gold                { get; set; }
        public int      Health              { get; set; }

        private int     attack;
        public  int     Attack              { get { return attack + Head.Attack + Body.Attack + Hand.Attack + Feet.Attack; }                set { attack = value; } }
        private int     attackChance;
        public  int     AttackChance        { get { return attackChance + Head.AttackChance + Body.AttackChance + Hand.AttackChance + Feet.AttackChance; } set { attackChance = value; } }
        private int     defense;
        public  int     Defense             { get { return defense + Head.Defense + Body.Defense + Hand.Defense + Feet.Defense; }           set { defense = value; } }
        private int     defenseChance;
        public  int     DefenseChance       { get { return defenseChance + Head.DefenseChance + Body.DefenseChance + Hand.DefenseChance + Feet.DefenseChance; } set { defenseChance = value; } }
        private int     maxHealth;
        public  int     MaxHealth           { get { return maxHealth + Head.MaxHealth + Body.MaxHealth + Hand.MaxHealth + Feet.MaxHealth; } set { maxHealth = value; } }
        private int     speed;
        public  int     Speed               { get { return speed + Head.Speed + Body.Speed + Hand.Speed + Feet.Speed; }                     set { speed = value; } }
        private int     awareness;
        public  int     Awareness           { get { return awareness + Head.Awareness + Body.Awareness + Hand.Awareness + Feet.Awareness; } set { awareness = value; } }

        public HeadEquipment    Head        { get; set; }
        public BodyEquipment    Body        { get; set; }
        public HandEquipment    Hand        { get; set; }
        public FeetEquipment    Feet        { get; set; }

        public IAbility         QAbility    { get; set; }
        public IAbility         WAbility    { get; set; }
        public IAbility         EAbility    { get; set; }
        public IAbility         RAbility    { get; set; }

        public IItem            Item1       { get; set; }
        public IItem            Item2       { get; set; }
        public IItem            Item3       { get; set; }
        public IItem            Item4       { get; set; }

        public List<IEffect>    Effects     { get; set; }
        public int              MaxEffects  { get; set; }

        public List<IInventory> Inventory   { get; set; }
        public int              MaxWeight   { get; set; }

        public bool             CanGrabTreasure { get; set; }

        // IDrawable
        public Colors       Color           { get; set; }
        public char         Symbol          { get; set; }
        public int          X               { get; set; }
        public int          Y               { get; set; }

        // Ischeduleable
        public  int         Time            { get {return Speed;} }

        public Game          Game           { get; protected set; }
        public DungeonMap    world          { get; protected set; }
        public FieldOfView   FieldOfView    { get; protected set; }

        public Actor(Game game)
        {
            Game        = game;

            Head        = HeadEquipment.None(game);
            Body        = BodyEquipment.None(game);
            Hand        = HandEquipment.None(game);
            Feet        = FeetEquipment.None(game);

            QAbility    = new DoNothing(game, this);
            WAbility    = new DoNothing(game, this);
            EAbility    = new DoNothing(game, this);
            RAbility    = new DoNothing(game, this);

            Item1       = new NoItem(game);
            Item2       = new NoItem(game);
            Item3       = new NoItem(game);
            Item4       = new NoItem(game);
                
            Effects     = new List<IEffect>();
            MaxEffects  = 4;                            //Should not be hardcoded in the future
        }

        public void SetMapAwareness()
        {
            world = Game.World;
            FieldOfView = new FieldOfView(world);
        }

        public void Draw(IMap map)
        {
            // Only draw the actor with the color and symbol when they are in field-of-view
            if (map.IsInFov(X, Y))
            {
                Game.SetMapCell(X, Y, Color, Colors.FloorBackgroundFov, Symbol, map.GetCell(X, Y).IsExplored);
            }
            else
            {
                // When not in field-of-view just draw a normal floor
                Game.SetMapCell(X, Y, Colors.Floor, Colors.FloorBackground, '.', map.GetCell(X, Y).IsExplored);
            }
        }

        public bool AddAbility(IAbility ability)
        {
            if (QAbility is DoNothing)
            {
                ability.Owner = this;
                QAbility = ability;
            }
            else if (WAbility is DoNothing)
            {
                ability.Owner = this;
                WAbility = ability;
            }
            else if (EAbility is DoNothing)
            {
                ability.Owner = this;
                EAbility = ability;
            }
            else if (RAbility is DoNothing)
            {
                ability.Owner = this;
                RAbility = ability;
            }
            else
            {
                return false;
            }

            return true;
        }

        public bool HaveAnyAbility()
        {
            if(QAbility is DoNothing && WAbility is DoNothing && EAbility is DoNothing && RAbility is DoNothing)
            {
                return false;
            }

            return true;
        }

        public bool AddItem(IItem item)
        {
            if (Item1 is NoItem)
            {
                item.Owner = this;
                Item1 = item;
            }
            else if (Item2 is NoItem)
            {
                item.Owner = this;
                Item2 = item;
            }
            else if (Item3 is NoItem)
            {
                item.Owner = this;
                Item3 = item;
            }
            else if (Item4 is NoItem)
            {
                item.Owner = this;
                Item4 = item;
            }
            else
            {
                return false;
            }

            return true;
        }

        protected bool UseItem(int itemNum)
        {
            bool didUseItem = false;

            if (itemNum == 1)
            {
                if (Item1.Owner == null) Item1.Owner = this;
                didUseItem = Item1.Use();
            }
            else if (itemNum == 2)
            {
                if (Item2.Owner == null) Item2.Owner = this;
                didUseItem = Item2.Use();
            }
            else if (itemNum == 3)
            {
                if (Item3.Owner == null) Item3.Owner = this;
                didUseItem = Item3.Use();
            }
            else if (itemNum == 4)
            {
                if (Item4.Owner == null) Item4.Owner = this;
                didUseItem = Item4.Use();
            }

            if (didUseItem)
            {
                RemoveItemsWithNoRemainingUses();
            }

            return didUseItem;
        }

        public void RemoveItemsWithNoRemainingUses()
        {
            if (Item1.RemainingUses <= 0)
            {
                Item1 = new NoItem(Game);
            }
            if (Item2.RemainingUses <= 0)
            {
                Item2 = new NoItem(Game);
            }
            if (Item3.RemainingUses <= 0)
            {
                Item3 = new NoItem(Game);
            }
            if (Item4.RemainingUses <= 0)
            {
                Item4 = new NoItem(Game);
            }
        }

        public bool RemoveEffect(IEffect effect)
        {
            Effects.Remove(effect);
            return true;
        }

        public bool AddEffect(IEffect effect)
        {
            foreach(IEffect currentEffect in Effects)
            {
                if(effect == currentEffect)
                {
                    currentEffect.Duration += effect.Duration;
                    return true;
                }
            }

            if(Effects.Count < MaxEffects)
            {
                Effects.Add(effect);
            }

            return true;
        }

        public void Tick()
        {
            QAbility?.Tick();
            WAbility?.Tick();
            EAbility?.Tick();
            RAbility?.Tick();

            for (int i = 0; i < Effects.Count; i++)
            {
                ((Effect)Effects[i])?.Perform();
                Effects[i]?.Tick();

                if(Effects[i].Duration == 0)
                {
                    RemoveEffect(Effects[i]);
                    i--;
                }
            }
        }

        public virtual bool PerformAction(InputCommands command)
        {
            return false;
        }
    }
}