using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace FruitSort
{
    /// <summary>
    /// Hiệu ứng rót nước khi pha: nâng & nghiêng bình nguồn trên miệng bình đích, tạo một tia
    /// nước chảy xuống, mực nước đích dâng dần trong khi nguồn vơi, kèm vài giọt bắn. Khi xong
    /// finalize màu/thể tích rồi đưa bình nguồn về chỗ. Đặt 1 cái trong scene (singleton).
    /// </summary>
    public class JuicePourFX : MonoBehaviour
    {
        public static JuicePourFX Instance { get; private set; }

        [Header("Sprites")]
        public Sprite streamSprite;
        public Sprite dropletSprite;

        [Header("Tuning")]
        [Tooltip("Thời lượng giai đoạn nước chảy.")]
        public float pourDuration = 0.6f;
        [Tooltip("Góc nghiêng bình nguồn khi rót (độ).")]
        public float tiltAngle = 100f;
        [Tooltip("Bình nguồn đứng bên nào của bình đích: +1 = phải (nghiêng sang trái), -1 = trái.")]
        public float pourSide = 1f;
        [Tooltip("Vòi bình nguồn cao hơn miệng bình đích bao nhiêu khi rót.")]
        public float pourHeight = 0.35f;
        [Tooltip("Bề rộng tia nước (world unit).")]
        public float streamWidth = 0.12f;
        [Tooltip("Miệng bình đích cao hơn tâm bao nhiêu (nơi tia rơi xuống).")]
        public float targetMouthUp = 0.6f;
        [Tooltip("Vòi bình nguồn cách tâm bao nhiêu (nơi nước trào ra).")]
        public float sourceSpoutUp = 0.62f;
        public int sortingOrder = 130;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Rót (pha) bình nguồn vào bình đích kèm hiệu ứng. Nếu không hợp lệ thì đưa nguồn về.</summary>
        public void Pour(JuiceBottle source, JuiceBottle target)
        {
            if (source == null || target == null || source == target || source.IsEmpty)
            {
                if (source != null) source.ReturnHome();
                return;
            }
            StartCoroutine(PourRoutine(source, target));
        }

        IEnumerator PourRoutine(JuiceBottle source, JuiceBottle target)
        {
            source.IsBusy = true;
            target.IsBusy = true;

            // Trạng thái đầu & kết quả cuối.
            Color tgtC0 = target.juiceColor; float tgtV0 = target.volume;
            Color srcC0 = source.juiceColor; float srcV0 = source.volume;
            Color finalC; float finalV;
            if (target.IsEmpty)
            {
                finalC = srcC0;
                finalV = Mathf.Min(target.maxVolume, srcV0);
            }
            else
            {
                finalC = JuiceColorUtility.MixRGB(tgtC0, tgtV0, srcC0, srcV0);
                finalV = Mathf.Min(target.maxVolume, tgtV0 + srcV0);
            }

            // Tính NGƯỢC vị trí bình nguồn sao cho VÒI nằm ngay trên MIỆNG bình đích.
            float ang = pourSide * tiltAngle;
            Quaternion tiltRot = Quaternion.Euler(0f, 0f, ang);
            Vector3 upAfter = tiltRot * Vector3.up;                       // hướng vòi sau khi nghiêng
            Vector3 mouth0 = target.transform.position + Vector3.up * targetMouthUp;
            Vector3 spoutGoal = mouth0 + Vector3.up * pourHeight;         // vòi nằm ngay trên miệng
            Vector3 pourPos = spoutGoal - upAfter * sourceSpoutUp;        // suy ra tâm bình nguồn

            // 1) Nâng & nghiêng bình nguồn.
            source.transform.DOKill();
            source.transform.DOMove(pourPos, 0.22f).SetEase(Ease.OutQuad);
            source.transform.DORotate(new Vector3(0f, 0f, ang), 0.22f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.22f);

            // 2) Tạo tia nước + bắn vài giọt (màu tia/giọt hiển thị tươi như trong bình).
            Color streamC = JuiceColorUtility.Vivid(srcC0);
            GameObject stream = CreateStream(streamC);
            SpawnSplash(target.transform.position + Vector3.up * targetMouthUp, streamC, 5);

            float t = 0f;
            float splashTimer = 0f;
            while (t < pourDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / pourDuration);

                target.SetJuice(Color.Lerp(tgtC0, finalC, k), Mathf.Lerp(tgtV0, finalV, k));
                source.SetJuice(srcC0, Mathf.Lerp(srcV0, 0f, k));

                UpdateStream(stream, source, target, streamC);

                splashTimer += Time.deltaTime;
                if (splashTimer >= 0.12f)
                {
                    splashTimer = 0f;
                    SpawnSplash(target.transform.position + Vector3.up * targetMouthUp, streamC, 2);
                }
                yield return null;
            }

            // 3) Finalize.
            target.SetJuice(finalC, finalV);
            source.Empty();
            if (stream != null) Destroy(stream);

            // 4) Dựng thẳng & về chỗ. KHÔNG dùng ReturnHome vì DOKill bên trong nó sẽ huỷ
            //    luôn tween xoay-thẳng -> bình kẹt ở góc nghiêng. Tự DOKill 1 lần rồi move+rotate.
            target.transform.DOpunchScaleSafe();
            source.transform.DOKill();
            Vector3 home = source.homeSlot != null ? source.homeSlot.position : source.transform.position;
            source.transform.DOMove(home, 0.28f).SetEase(Ease.OutQuad);
            source.transform.DORotate(Vector3.zero, 0.28f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.3f);

            source.transform.rotation = Quaternion.identity; // chắc chắn về thẳng đứng
            source.IsBusy = false;
            target.IsBusy = false;
        }

        GameObject CreateStream(Color color)
        {
            var go = new GameObject("PourStream");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = streamSprite;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        void UpdateStream(GameObject stream, JuiceBottle source, JuiceBottle target, Color color)
        {
            if (stream == null || streamSprite == null) return;
            // Đầu tia = đỉnh bình nguồn (đã nghiêng), cuối tia = miệng bình đích.
            Vector3 spout = source.transform.position + source.transform.up * sourceSpoutUp;
            Vector3 mouth = target.transform.position + Vector3.up * targetMouthUp;

            Vector3 mid = (spout + mouth) * 0.5f;
            mid.z = -0.2f;
            stream.transform.position = mid;

            Vector3 dir = mouth - spout;
            float len = dir.magnitude;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f; // sprite dọc (+y)
            stream.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            Vector2 sp = streamSprite.bounds.size;
            float sx = sp.x > 0.001f ? streamWidth / sp.x : 1f;
            float sy = sp.y > 0.001f ? len / sp.y : 1f;
            stream.transform.localScale = new Vector3(sx, sy, 1f);
        }

        void SpawnSplash(Vector3 pos, Color color, int count)
        {
            if (dropletSprite == null) return;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Droplet");
                go.transform.position = pos;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dropletSprite;
                sr.color = color;
                sr.sortingOrder = sortingOrder + 1;
                float sc = Random.Range(0.12f, 0.22f);
                go.transform.localScale = Vector3.one * sc;

                Vector3 dst = pos + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.5f, -0.1f), 0f);
                go.transform.DOMove(dst, Random.Range(0.25f, 0.45f)).SetEase(Ease.OutQuad);
                sr.DOFade(0f, 0.4f).OnComplete(() => { if (go != null) Destroy(go); });
            }
        }
    }

    static class PourTweenExt
    {
        public static void DOpunchScaleSafe(this Transform t)
        {
            t.DOKill(true);
            t.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.6f);
        }
    }
}
