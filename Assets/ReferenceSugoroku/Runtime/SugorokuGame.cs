using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ReferenceSugoroku
{
    /// <summary>One turn = a cosmetic roll followed by exactly the sampled number of steps.</summary>
    public sealed class SugorokuGame : MonoBehaviour
    {
        public Transform pawn;
        public Transform die;
        public Transform[] route;
        public Button rollButton;
        public Text resultLabel;
        public GameObject menuPanel;
        [Min(0)] public int startingIndex;
        [Min(0.05f)] public float stepSeconds = 0.28f;
        [Min(0f)] public float hopHeight = 0.18f;
        [Min(0.1f)] public float rollSeconds = 0.8f;
        public Vector3 dieRestEuler = new Vector3(0f, 18f, 0f);
        [Range(1, 6)] public int initialFace = 1; // 開始時・リセット時に上を向く目

        public bool IsBusy { get; private set; }
        public bool IsPaused { get; private set; }
        public int CurrentIndex { get; private set; }
        public int LastRoll { get; private set; }
        Vector3 dieRestPosition;
        Quaternion dieRestRotation;
        Coroutine turn;

        // 開始時・リセット時のサイコロの回転(initialFace が上を向き、向きは dieRestEuler)
        public Quaternion InitialDieRotation { get { return Quaternion.Euler(dieRestEuler) * FaceUpRotation(initialFace); } }

        // 編集中(Play モードに入る前)もサイコロを initialFace に合わせる
        void OnValidate()
        {
            if (!Application.isPlaying && die) die.localRotation = InitialDieRotation;
        }

        void OnEnable() { RefreshButton(); }

        void Start()
        {
            if (!pawn || !die || route == null || route.Length < 2)
            {
                Debug.LogError("Sugoroku: pawn, die and at least two route points are required.", this);
                if (rollButton) rollButton.interactable = false;
                enabled = false;
                return;
            }
            foreach (Transform point in route)
                if (!point) { Debug.LogError("Sugoroku: route contains a missing point.", this); enabled = false; if (rollButton) rollButton.interactable = false; return; }
            dieRestPosition = die.localPosition;
            dieRestRotation = Quaternion.Euler(dieRestEuler);
            ResetGame();
        }

        public void Roll()
        {
            // Guard before starting coroutine: repeated UI/pointer events cannot queue turns.
            if (!enabled || IsBusy || IsPaused || route == null || route.Length < 2) return;
            IsBusy = true;
            RefreshButton();
            LastRoll = Random.Range(1, 7);
            turn = StartCoroutine(PlayTurn(LastRoll));
        }

        IEnumerator PlayTurn(int result)
        {
            if (resultLabel) resultLabel.text = "…";
            Quaternion start = die.localRotation;
            float elapsed = 0f;
            while (elapsed < rollSeconds)
            {
                if (!IsPaused)
                {
                    elapsed = Mathf.Min(rollSeconds, elapsed + Time.unscaledDeltaTime);
                    float t = elapsed / rollSeconds;
                    die.localRotation = start * Quaternion.Euler(720f * t, 540f * t, 360f * t);
                    die.localPosition = dieRestPosition + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.65f;
                }
                yield return null;
            }
            die.localPosition = dieRestPosition;
            die.localRotation = dieRestRotation * FaceUpRotation(result);
            if (resultLabel) resultLabel.text = result.ToString();
            for (int step = 0; step < result; step++)
            {
                int next = (CurrentIndex + 1) % route.Length;
                Vector3 from = pawn.position;
                Vector3 to = route[next].position;
                elapsed = 0f;
                while (elapsed < stepSeconds)
                {
                    if (!IsPaused)
                    {
                        elapsed = Mathf.Min(stepSeconds, elapsed + Time.unscaledDeltaTime);
                        float t = elapsed / stepSeconds;
                        pawn.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * hopHeight;
                    }
                    yield return null;
                }
                pawn.position = to;
                CurrentIndex = next;
            }
            turn = null;
            IsBusy = false;
            RefreshButton();
        }

        // Blender asset contract: 1=+X, 2=-Y, 3=-Z, 4=+Z, 5=+Y, 6=-X.
        public static Quaternion FaceUpRotation(int face)
        {
            switch (face)
            {
                case 1: return Quaternion.FromToRotation(Vector3.right, Vector3.up);
                case 2: return Quaternion.FromToRotation(Vector3.down, Vector3.up);
                case 3: return Quaternion.FromToRotation(Vector3.back, Vector3.up);
                case 4: return Quaternion.FromToRotation(Vector3.forward, Vector3.up);
                case 5: return Quaternion.identity;
                case 6: return Quaternion.FromToRotation(Vector3.left, Vector3.up);
                default: return Quaternion.identity;
            }
        }

        public void ToggleMenu() { SetPaused(!IsPaused); }
        public void Resume() { SetPaused(false); }
        void SetPaused(bool paused)
        {
            IsPaused = paused;
            if (menuPanel) menuPanel.SetActive(paused);
            RefreshButton();
            // Deliberately do not change global Time.timeScale (safe in a host project).
        }
        public void ResetGame()
        {
            if (!enabled || route == null || route.Length == 0 || !pawn || !die) return;
            if (turn != null) StopCoroutine(turn);
            turn = null;
            IsBusy = false;
            LastRoll = 0;
            CurrentIndex = Mathf.Clamp(startingIndex, 0, route.Length - 1);
            pawn.position = route[CurrentIndex].position;
            die.localPosition = dieRestPosition;
            die.localRotation = InitialDieRotation;
            if (resultLabel) resultLabel.text = "";
            SetPaused(false);
        }
        void RefreshButton() { if (rollButton) rollButton.interactable = isActiveAndEnabled && !IsBusy && !IsPaused; }
        void OnDisable()
        {
            if (turn != null) StopCoroutine(turn);
            turn = null;
            IsBusy = false;
            if (pawn && route != null && CurrentIndex < route.Length && route[CurrentIndex]) pawn.position = route[CurrentIndex].position;
            if (die) { die.localPosition = dieRestPosition; die.localRotation = InitialDieRotation; }
            SetPaused(false);
        }
    }
}
