using UnityEngine;

namespace Kakashi
{
    /// <summary>農家の人形の動き：歩き・身振り手振り・うなずき／首振り・口パク・カカシを見る視線。</summary>
    public class FarmerRig : MonoBehaviour
    {
        public enum Mode { Idle, Walk, Talk }
        public Mode mode = Mode.Idle;
        /// <summary>true の間だけ口が動く（しゃべっている最中）</summary>
        public bool speaking;
        /// <summary>0 = がっかり … 1 = 大絶賛。身振りの大きさ・うなずき／首振りが変わる。</summary>
        public float mood = 0.5f;
        /// <summary>見る相手（カカシ）</summary>
        public Transform lookAt;
        public float seed;
        /// <summary>道具を持っている腕（0=左 1=右）。この腕はあまり動かさない。</summary>
        public int toolArm = -1;

        public Transform vis, head, mouth;
        public Transform[] hip = new Transform[2];
        public Transform[] sh = new Transform[2];
        public Transform[] el = new Transform[2];
        public Quaternion[] shRest = new Quaternion[2];
        public Quaternion[] elRest = new Quaternion[2];

        float t, walkW, talkW;

        void Update()
        {
            if (vis == null || head == null) return;
            float dt = Time.deltaTime;
            t += dt;
            float ph = t + seed;
            walkW = Mathf.MoveTowards(walkW, mode == Mode.Walk ? 1f : 0f, dt * 6f);
            talkW = Mathf.MoveTowards(talkW, mode == Mode.Talk ? 1f : 0f, dt * 4f);
            bool happy = mood > 0.7f, sad = mood < 0.4f;

            // 体：歩きの上下・ゆれ／うれしい時のはずみ
            float walkPh = ph * 7.5f;
            float bob = Mathf.Abs(Mathf.Sin(walkPh)) * 0.07f * walkW;
            if (happy) bob += Mathf.Abs(Mathf.Sin(ph * 5f)) * 0.09f * talkW;
            float roll = Mathf.Sin(walkPh) * 3.5f * walkW + Mathf.Sin(ph * 1.3f) * 1.2f;
            vis.localPosition = new Vector3(0f, bob, 0f);
            vis.localRotation = Quaternion.Euler(0f, 0f, roll);

            // 脚
            float stride = Mathf.Sin(walkPh) * 32f * walkW;
            if (hip[0] != null) hip[0].localRotation = Quaternion.Euler(stride, 0f, 0f);
            if (hip[1] != null) hip[1].localRotation = Quaternion.Euler(-stride, 0f, 0f);

            // 腕：歩きの腕振り＋しゃべりの身振り手振り
            for (int i = 0; i < 2; i++)
            {
                if (sh[i] == null || el[i] == null) continue;
                float side = i == 0 ? -1f : 1f;
                float amp = i == toolArm ? 0.12f : 1f;
                float dsx = Mathf.Sin(walkPh + (i == 0 ? 0f : Mathf.PI)) * 28f * walkW;
                float dsz = 0f, dex = 0f;
                float beat = 0.5f + 0.5f * Mathf.Sin(ph * (2.5f + i * 0.8f) + i * 1.7f);
                float beat2 = 0.5f + 0.5f * Mathf.Sin(ph * 1.8f + i * 2.9f + 1f);
                if (sad)
                {
                    // 「あちゃー」：両手を開いて肩をすくめる
                    dsx += -(20f + 18f * beat) * talkW;
                    dsz += side * (32f + 10f * beat2) * talkW;
                    dex += -(75f + 15f * beat) * talkW;
                }
                else
                {
                    float energy = happy ? 1.25f : 0.8f;
                    dsx += -(22f + 55f * beat) * energy * talkW;
                    dsz += side * (8f + 30f * beat2) * energy * talkW;
                    dex += -(12f + 62f * beat2) * energy * talkW;
                }
                sh[i].localRotation = shRest[i] * Quaternion.Euler(dsx * amp, 0f, dsz * amp);
                el[i].localRotation = elRest[i] * Quaternion.Euler(dex * amp, 0f, 0f);
            }

            // 頭：カカシを見たり、こちらを見たり／うなずき・首振り
            float yaw = 0f, pitch = 0f;
            if (lookAt != null)
            {
                Vector3 d = transform.InverseTransformPoint(lookAt.position);
                d.y = 0f;
                float to = Mathf.Clamp(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -65f, 65f);
                float glance = mode == Mode.Talk ? Mathf.Clamp01(Mathf.Sin(ph * 0.8f) * 1.5f + 0.6f)
                             : mode == Mode.Walk ? 0.15f : 1f;
                yaw = to * glance;
            }
            if (talkW > 0f)
            {
                if (happy) pitch += Mathf.Sin(ph * 6f) * 7f * talkW;
                else if (sad) yaw += Mathf.Sin(ph * 4.2f) * 16f * talkW;
                else pitch += Mathf.Sin(ph * 3.2f) * 4f * talkW;
            }
            head.localRotation = Quaternion.Euler(pitch, yaw, 0f);

            // 口パク
            if (mouth != null)
            {
                float open = speaking ? Mathf.Abs(Mathf.Sin(ph * 11f)) * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(ph * 4.1f + 1f))) : 0f;
                mouth.localScale = new Vector3(0.09f, 0.012f + 0.035f * open, 0.02f);
            }
        }
    }
}
