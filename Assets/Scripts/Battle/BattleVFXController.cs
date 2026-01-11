// Assets/Scripts/Battle/BattleVfxController.cs
using System.Collections;
using UnityEngine;

namespace Nebula
{
    public class BattleVfxController : MonoBehaviour
    {
        [Header("Sprite Renderers")]
        public SpriteRenderer playerPilot;
        public SpriteRenderer enemyPilot;
        public SpriteRenderer playerShip;
        public SpriteRenderer enemyShip;

        public SpriteRenderer playerMonster;
        public SpriteRenderer enemyMonster;

        [Header("Slide Settings")]
        public float slideDuration = 0.55f;
        public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float offscreenX = 12f; // world units now (tune to your camera)

        [Header("Hit Settings")]
        public float hitShakeDuration = 0.18f;
        public float hitShakeAmount = 0.25f; // world units now

        public float critShakeAmount = 0.45f;
        public float weakShakeAmount = 0.15f;

        [Header("Heal Settings")]
        public float healPunchDuration = 0.25f;
        public float healPunchScale = 1.12f;

        private Coroutine _playerAnimCo;
        private Coroutine _enemyAnimCo;

        public void SetupSprites(
            Sprite playerPilotSprite, Sprite playerShipSprite,
            Sprite enemyPilotSprite, Sprite enemyShipSprite,
            Sprite playerMonsterSprite, Sprite enemyMonsterSprite)
        {
            if (playerPilot) playerPilot.sprite = playerPilotSprite;
            if (playerShip) playerShip.sprite = playerShipSprite;
            if (enemyPilot) enemyPilot.sprite = enemyPilotSprite;
            if (enemyShip) enemyShip.sprite = enemyShipSprite;

            if (playerMonster) playerMonster.sprite = playerMonsterSprite;
            if (enemyMonster) enemyMonster.sprite = enemyMonsterSprite;
        }

        public IEnumerator PlayIntro()
        {
            // Start offscreen (x only)
            SetOffscreen(playerPilot?.transform, -offscreenX);
            SetOffscreen(playerShip?.transform, -offscreenX);
            SetOffscreen(enemyPilot?.transform, offscreenX);
            SetOffscreen(enemyShip?.transform, offscreenX);

            SetOffscreen(playerMonster?.transform, -offscreenX);
            SetOffscreen(enemyMonster?.transform, offscreenX);

            // 1) pilots + ships
            yield return SlideToX(playerPilot?.transform, 0f);
            yield return SlideToX(enemyPilot?.transform, 0f);

            yield return SlideToX(playerShip?.transform, 0f);
            yield return SlideToX(enemyShip?.transform, 0f);

            // 2) monsters
            yield return SlideToX(playerMonster?.transform, 0f);
            yield return SlideToX(enemyMonster?.transform, 0f);
        }

        public IEnumerator PlayOutro()
        {
            yield return SlideToX(playerMonster?.transform, -offscreenX);
            yield return SlideToX(enemyMonster?.transform, offscreenX);
            yield return SlideToX(playerShip?.transform, -offscreenX);
            yield return SlideToX(enemyShip?.transform, offscreenX);
            yield return SlideToX(playerPilot?.transform, -offscreenX);
            yield return SlideToX(enemyPilot?.transform, offscreenX);
        }

        public void StartMonsterAnim(SpriteRenderer sr, MonsterDefinition def, bool isPlayerSide)
        {
            if (sr == null || def == null || def.animFrames == null || def.animFrames.Count == 0)
                return;

            if (isPlayerSide)
            {
                if (_playerAnimCo != null) StopCoroutine(_playerAnimCo);
                _playerAnimCo = StartCoroutine(AnimLoop(sr, def));
            }
            else
            {
                if (_enemyAnimCo != null) StopCoroutine(_enemyAnimCo);
                _enemyAnimCo = StartCoroutine(AnimLoop(sr, def));
            }
        }

        private IEnumerator AnimLoop(SpriteRenderer sr, MonsterDefinition def)
        {
            int fps = Mathf.Max(1, def.animFps);
            float dt = 1f / fps;
            int i = 0;

            while (true)
            {
                sr.sprite = def.animFrames[i % def.animFrames.Count];
                i++;
                yield return new WaitForSeconds(dt);
            }
        }

        public IEnumerator PlayHit(SpriteRenderer target, bool crit, bool weak, ElementType element)
        {
            if (target == null) yield break;

            float amt = hitShakeAmount;
            if (crit) amt = critShakeAmount;
            else if (weak) amt = weakShakeAmount;

            // element-tailored “feel”
            switch (element)
            {
                case ElementType.Solar: amt *= 1.1f; break;
                case ElementType.Void: amt *= 0.9f; break;
                case ElementType.Bio: amt *= 1.0f; break;
                case ElementType.Time: amt *= 1.0f; break;
            }

            yield return ShakeTransform(target.transform, hitShakeDuration, amt);

            if (element == ElementType.Time)
                yield return ShakeTransform(target.transform, hitShakeDuration * 0.6f, amt * 0.6f);
        }

        public IEnumerator PlayHeal(SpriteRenderer target)
        {
            if (target == null) yield break;
            yield return ScalePunch(target.transform, healPunchDuration, healPunchScale);
        }

        private static void SetOffscreen(Transform t, float x)
        {
            if (!t) return;
            var p = t.position;
            p.x = x;
            t.position = p;
        }

        private IEnumerator SlideToX(Transform t, float x)
        {
            if (!t) yield break;

            Vector3 start = t.position;
            Vector3 end = new Vector3(x, start.y, start.z);

            float t01 = 0f;
            while (t01 < 1f)
            {
                t01 += Time.deltaTime / Mathf.Max(0.001f, slideDuration);
                float k = slideCurve.Evaluate(Mathf.Clamp01(t01));
                t.position = Vector3.LerpUnclamped(start, end, k);
                yield return null;
            }

            t.position = end;
        }

        private static IEnumerator ShakeTransform(Transform t, float duration, float amount)
        {
            if (!t) yield break;

            Vector3 basePos = t.position;
            float tt = 0f;

            while (tt < duration)
            {
                tt += Time.deltaTime;
                float strength = Mathf.Lerp(amount, 0f, tt / Mathf.Max(0.0001f, duration));

                Vector2 jitter = Random.insideUnitCircle * strength;
                t.position = basePos + new Vector3(jitter.x, jitter.y, 0f);

                yield return null;
            }

            t.position = basePos;
        }

        private static IEnumerator ScalePunch(Transform t, float duration, float scaleUp)
        {
            if (!t) yield break;

            Vector3 baseS = t.localScale;
            Vector3 upS = baseS * scaleUp;

            float half = duration * 0.5f;

            float tt = 0f;
            while (tt < half)
            {
                tt += Time.deltaTime;
                t.localScale = Vector3.Lerp(baseS, upS, tt / Mathf.Max(0.0001f, half));
                yield return null;
            }

            tt = 0f;
            while (tt < half)
            {
                tt += Time.deltaTime;
                t.localScale = Vector3.Lerp(upS, baseS, tt / Mathf.Max(0.0001f, half));
                yield return null;
            }

            t.localScale = baseS;
        }
    }
}
