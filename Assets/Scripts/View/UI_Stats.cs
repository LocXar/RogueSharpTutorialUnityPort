﻿using UnityEngine;
using UnityEngine.UI;
using RogueSharpTutorial.Model;
using RogueSharpTutorial.Controller;

namespace RogueSharpTutorial.View
{
    public class UI_Stats : MonoBehaviour
    {
        [SerializeField] private Text                   nameField;
        [SerializeField] private Text                   healthField;
        [SerializeField] private Text                   attackField;
        [SerializeField] private Text                   defenseField;
        [SerializeField] private Text                   goldField;
        [SerializeField] private Text                   mapLevelField;

        [SerializeField] private VerticalLayoutGroup    enemyGroup;
        [SerializeField] private GameObject             statBarPrefab;

        /// <summary>
        /// Update stats section of the screen for the player stats.
        /// </summary>
        /// <param name="game"></param>
        public void DrawPlayerStats(Game game)
        {
            if(game == null)
            {
                return;
            }

            nameField.color = healthField.color = attackField.color = defenseField.color = mapLevelField.color = ColorMap.UnityColors[Colors.Text];
            goldField.color = ColorMap.UnityColors[Colors.Gold];

            nameField.text      = "Name:    " + game.Player.Name;
            healthField.text    = "Health:   " + game.Player.CurrentHealth + "/" + game.Player.MaxHealthAdjusted;
            attackField.text    = "Attack:   " + game.Player.AttackMeleeAdjusted + " (" + game.Player.AttackChanceMeleeAdjusted + "%)";
            defenseField.text   = "Defense: " + game.Player.DefenseAdjusted + " (" + game.Player.DefenseChanceAdjusted + "%)";
            goldField.text      = "Gold:      " + game.Player.Gold;
            mapLevelField.text  = "Map Level: " + game.mapLevel;
        }

        /// <summary>
        /// Update the Monsters section of stats screen dynamically based on the number of Monsters in the Player's FOV.
        /// </summary>
        /// <param name="monster"></param>
        /// <param name="position"></param>
        public void DrawMonsterStats(Monster monster, int position)
        {
            if(position == 0)
            {
                ClearMonsterStats();
            }

            GameObject obj = Instantiate(statBarPrefab);
            obj.transform.SetParent(enemyGroup.transform, false);

            StatBar bar = obj.GetComponent<StatBar>();

            bar.SetSlider(monster.CurrentHealth, monster.MaxHealthAdjusted, monster.Name, ColorMap.UnityColors[monster.Color], monster.Symbol);
        }

        /// <summary>
        /// Clear all Monsters from the stats section of the screen. Necessary when there was one monster in the Player's FOV and then none were.
        /// </summary>
        public void ClearMonsterStats()
        {
            foreach (Transform child in enemyGroup.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
