using System.Collections;
using UnityEngine;

namespace Nebula
{
    /// <summary>
    /// Example battle story sequences.
    /// Copy and modify these for your own story beats.
    ///
    /// Usage in BattleController or elsewhere:
    ///     var seq = GetComponent<ExampleBattleSequences>();
    ///     yield return seq.PlayPirateIntro(pirateCharacter, pirateMonster);
    /// </summary>
    public class ExampleBattleSequences : MonoBehaviour
    {
        [Header("Player References")]
        public CharacterDefinition playerCharacter;
        public ShipDefinition playerShip;

        [Header("Audio")]
        public NPCChirpProfile playerChirp;
        public NPCChirpProfile enemyChirp;

        private BattleStoryDirector Story => BattleStoryDirector.Instance;

        /// <summary>
        /// Example: Pirate encounter intro.
        /// </summary>
        public IEnumerator PlayPirateIntro(CharacterDefinition pirate, MonsterDefinition pirateMonster)
        {
            // Start with player ship visible
            yield return Story.ShowShip(playerShip, waitForCard: true);
            yield return Story.Say("", "You're cruising through the asteroid field...");

            // Enemy appears
            yield return Story.HideShip();
            yield return Story.ShowCharacter(pirate, waitForCard: true);
            yield return Story.Say(pirate.displayName, "Halt! This sector belongs to me!", enemyChirp);

            // Player responds
            yield return Story.HideCharacter();
            yield return Story.ShowCharacter(playerCharacter, waitForCard: true);
            yield return Story.Say(playerCharacter.displayName, "I don't think so!", playerChirp);

            // Show the monster they're sending out
            yield return Story.HideCharacter();
            yield return Story.ShowMonster(pirateMonster, waitForCard: true);
            yield return Story.Say(pirate.displayName, $"Go, {pirateMonster.displayName}!");

            // Clear for battle
            yield return Story.Wait(0.3f);
            yield return Story.ClearText();
            // Keep monster visible for battle start, or:
            // yield return Story.HideMonster();
        }

        /// <summary>
        /// Example: Boss encounter intro.
        /// </summary>
        public IEnumerator PlayBossIntro(CharacterDefinition boss, MonsterDefinition bossMonster)
        {
            // Dramatic pause
            yield return Story.SayInstant("", "...");
            yield return Story.Wait(1f);

            // Boss appears
            yield return Story.ShowCharacter(boss, waitForCard: true);
            yield return Story.Say(boss.displayName, "So, you've finally arrived.");
            yield return Story.Say(boss.displayName, "I've been waiting for this moment.");

            // Show their powerful monster
            yield return Story.ShowMonster(bossMonster, waitForCard: true);
            yield return Story.Say(boss.displayName, $"Witness true power!");

            yield return Story.ClearText();
        }

        /// <summary>
        /// Example: Victory dialogue.
        /// </summary>
        public IEnumerator PlayVictory(CharacterDefinition defeatedEnemy)
        {
            yield return Story.HideMonster();
            yield return Story.ShowCharacter(defeatedEnemy, waitForCard: true);
            yield return Story.Say(defeatedEnemy.displayName, "Impossible... I lost?!");

            yield return Story.HideCharacter();
            yield return Story.ShowCharacter(playerCharacter, waitForCard: true);
            yield return Story.Say(playerCharacter.displayName, "Better luck next time!");

            yield return Story.ClearText();
            yield return Story.HideAllCards();
        }

        /// <summary>
        /// Example: Simple narration (no characters).
        /// </summary>
        public IEnumerator PlayNarration(string[] lines)
        {
            yield return Story.HideAllCards();

            foreach (var line in lines)
            {
                yield return Story.Say("", line);
            }

            yield return Story.ClearText();
        }

        /// <summary>
        /// Example: Quick monster introduction (no dialogue).
        /// </summary>
        public IEnumerator ShowMonsterQuick(MonsterDefinition monster, float duration = 1.5f)
        {
            yield return Story.ShowMonster(monster, waitForCard: true);
            yield return Story.Wait(duration);
            yield return Story.HideMonster();
        }
    }
}
