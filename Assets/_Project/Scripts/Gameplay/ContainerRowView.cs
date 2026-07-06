using System.Collections.Generic;
using CubeBurst.Core;
using CubeBurst.Systems;
using DG.Tweening;
using UnityEngine;

namespace CubeBurst.Gameplay
{
    /// The container area under the tray, reference style: four active
    /// "loaf" pills with three socket holes each, and the upcoming queue
    /// stacked in rows below them.
    public class ContainerRowView : MonoBehaviour
    {
        static readonly float[] SlotX = { -2.7f, -0.9f, 0.9f, 2.7f };
        const float ActiveY = -2.65f;
        const float RowStep = 0.8f;
        const int QueueRows = 6;
        internal static readonly Vector3 PillScale = new Vector3(1.08f, 0.89f, 1f);

        // time the completed container's ghost takes to zoom away; the next
        // container and the queue shift wait this long before moving in
        const float GhostFlyTime = 0.34f;

        GameSession _session;
        readonly ContainerSlotView[] _slots = new ContainerSlotView[ContainerManagerModel.SlotCount];
        readonly List<GameObject> _queuePills = new List<GameObject>();
        readonly bool[] _justCompleted = new bool[ContainerManagerModel.SlotCount];

        public static ContainerRowView Create(Transform parent, GameSession session)
        {
            var go = new GameObject("ContainerRow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, ActiveY, 0f);

            var view = go.AddComponent<ContainerRowView>();
            view._session = session;
            for (int i = 0; i < view._slots.Length; i++)
                view._slots[i] = ContainerSlotView.Create(go.transform, new Vector3(SlotX[i], 0f, 0f));

            session.Containers.ContainerEntered += view.OnEntered;
            session.Containers.ContainerCompleted += view.OnCompleted;
            return view;
        }

        void OnDestroy()
        {
            if (_session == null) return;
            _session.Containers.ContainerEntered -= OnEntered;
            _session.Containers.ContainerCompleted -= OnCompleted;
        }

        void OnEntered(int slot, ContainerModel model)
        {
            // if this slot just completed, hold the incoming container + queue
            // shift until the outgoing ghost has flown off; otherwise (initial
            // fill) show it right away
            float delay = _justCompleted[slot] ? GhostFlyTime : 0f;
            _justCompleted[slot] = false;
            _slots[slot].SetContainer(model, delay);
            if (delay > 0f) DOVirtual.DelayedCall(delay, RebuildQueueDisplay);
            else RebuildQueueDisplay();
        }

        void OnCompleted(int slot, ContainerModel model)
        {
            _slots[slot].PlayCompleteGhost();
            _justCompleted[slot] = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayComplete();
        }

        /// Round-robin the global queue under the four columns so it reads
        /// like the reference's per-column stacks.
        void RebuildQueueDisplay()
        {
            foreach (var p in _queuePills) if (p != null) Destroy(p);
            _queuePills.Clear();

            var queue = _session.Containers.QueueSnapshot();
            for (int i = 0; i < queue.Length; i++)
            {
                int col = i % SlotX.Length, row = i / SlotX.Length;
                if (row >= QueueRows) break;
                var pill = CreatePillSprite(transform, Palette.Of(queue[i].Color), 61);
                pill.name = "QueuePill";
                pill.transform.localPosition = new Vector3(SlotX[col], -(row + 1) * RowStep, 0.1f);
                pill.transform.localScale = PillScale;
                _queuePills.Add(pill);
            }
        }

        internal static GameObject CreatePillSprite(Transform parent, Color color, int order)
        {
            var go = new GameObject("Pill", typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Pill();
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        public Vector3 GetBallPoint(int slot, int socketIndex) => _slots[slot].SocketPoint(socketIndex);

        public void RefreshSlot(int slot) => _slots[slot].RefreshDots();
    }

    /// One active container: a colored pill with capacity socket holes that
    /// fill up with balls.
    public class ContainerSlotView : MonoBehaviour
    {
        const float SocketSpacing = 0.42f;

        ContainerModel _model;
        GameObject _visual;
        SpriteRenderer _pill;
        readonly List<GameObject> _sockets = new List<GameObject>();
        readonly List<MeshRenderer> _fillBalls = new List<MeshRenderer>();

        /// World position of one hole, so each ball flies to its own socket
        /// instead of piling onto the container's center.
        public Vector3 SocketPoint(int socketIndex)
        {
            int cap = _model != null ? _model.Capacity : 3;
            if (socketIndex < 0) socketIndex = 0;
            float x = (socketIndex - (cap - 1) * 0.5f) * SocketSpacing;
            return transform.position + new Vector3(x, 0.05f, -0.2f);
        }

        public static ContainerSlotView Create(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("ContainerSlot");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var view = go.AddComponent<ContainerSlotView>();
            view._visual = new GameObject("Visual");
            view._visual.transform.SetParent(go.transform, false);

            var pillGo = ContainerRowView.CreatePillSprite(view._visual.transform, Color.white, 62);
            pillGo.transform.localScale = ContainerRowView.PillScale;
            view._pill = pillGo.GetComponent<SpriteRenderer>();

            view._visual.SetActive(false);
            return view;
        }

        /// The model + sockets are set immediately (so an early-arriving ball
        /// updates the right slot), but the pop-in animation is held for
        /// appearDelay so it doesn't overlap the outgoing container's ghost.
        public void SetContainer(ContainerModel model, float appearDelay)
        {
            _model = model;
            _visual.SetActive(model != null);
            if (model == null) return;

            _pill.color = Palette.Of(model.Color);
            RebuildSockets(model.Capacity);
            RefreshDots();
            _visual.transform.DOKill();
            _visual.transform.localScale = Vector3.zero; // stays invisible during the delay
            _visual.transform.DOScale(1f, 0.32f).SetEase(Ease.OutBack).SetDelay(appearDelay + 0.02f);
        }

        void RebuildSockets(int capacity)
        {
            foreach (var s in _sockets) if (s != null) Destroy(s);
            _sockets.Clear();
            foreach (var b in _fillBalls) if (b != null) Destroy(b.gameObject);
            _fillBalls.Clear();

            for (int i = 0; i < capacity; i++)
            {
                float x = (i - (capacity - 1) * 0.5f) * SocketSpacing;

                var socket = new GameObject("Socket", typeof(SpriteRenderer));
                socket.transform.SetParent(_visual.transform, false);
                socket.transform.localPosition = new Vector3(x, 0f, -0.05f);
                socket.transform.localScale = Vector3.one * 0.88f;
                var sr = socket.GetComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Socket();
                sr.sortingOrder = 63;
                _sockets.Add(socket);

                var ballGo = new GameObject("FillBall", typeof(MeshFilter), typeof(MeshRenderer));
                ballGo.transform.SetParent(_visual.transform, false);
                ballGo.transform.localPosition = new Vector3(x, 0.02f, -0.1f);
                ballGo.transform.localScale = Vector3.one * 0.3f;
                ballGo.GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.Sphere();
                var mr = ballGo.GetComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.sortingOrder = 64;
                _fillBalls.Add(mr);
            }
        }

        public void RefreshDots()
        {
            if (_model == null) return;
            for (int i = 0; i < _fillBalls.Count; i++)
            {
                bool filled = i < _model.Filled;
                _fillBalls[i].gameObject.SetActive(filled);
                if (filled) _fillBalls[i].sharedMaterial = CubeMeshFactory.BallMaterialFor(_model.Color);
            }
        }

        /// On completion the filled container snaps shut (a quick squash, like
        /// a lid closing) and then launches up and away as a detached ghost,
        /// while the real slot is immediately reused for the next container.
        public void PlayCompleteGhost()
        {
            // the completing ball fires this before the view refreshes, so make
            // sure all holes show filled before we clone the box to fly away
            RefreshDots();
            var ghost = Instantiate(_visual, _visual.transform.position, Quaternion.identity);
            ghost.name = "Ghost";
            ghost.SetActive(true);
            ghost.transform.localScale = Vector3.one;
            foreach (var sr in ghost.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder += 10;

            var t = ghost.transform;
            var seq = DOTween.Sequence();
            seq.Append(t.DOScale(new Vector3(1.14f, 0.72f, 1f), 0.1f).SetEase(Ease.OutQuad)); // close
            seq.Append(t.DOScale(Vector3.one, 0.05f).SetEase(Ease.OutQuad));                  // settle
            seq.Append(t.DOMove(t.position + new Vector3(0f, 1.8f, 0f), 0.24f).SetEase(Ease.InBack)); // launch up
            seq.Join(t.DOScale(0.12f, 0.24f).SetEase(Ease.InBack));                           // shrink away
            seq.OnComplete(() => { if (ghost != null) Destroy(ghost); });
            Destroy(ghost, 2f); // safety net if the tween is killed (OnComplete handles the normal case)
        }
    }
}
