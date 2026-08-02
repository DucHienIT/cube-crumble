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
        const float RowStep = 0.85f; // +15% vertical gap so stacked queue pills never touch
        const int QueueRows = 6;
        // pills are real 3D loaf meshes now; a small forward tilt reveals the
        // dark side wall (their thickness) so they read as solid objects
        internal static readonly Vector3 PillTilt = new Vector3(16f, 0f, 0f);

        // completion choreography: the full container squashes shut, hops out
        // of its slot and pops away; the front queue pill starts flying into
        // the freed slot mid-hop while the rest of the queue slides up with it
        const float GhostCloseTime = 0.17f; // squash + rebound, slot still occupied
        const float PillFlyTime = 0.3f;
        const float QueueShiftTime = 0.3f;

        GameSession _session;
        readonly ContainerSlotView[] _slots = new ContainerSlotView[ContainerManagerModel.SlotCount];
        readonly Dictionary<ContainerModel, Transform> _queuePills = new Dictionary<ContainerModel, Transform>();
        readonly List<ContainerModel> _staleScratch = new List<ContainerModel>();
        readonly bool[] _justCompleted = new bool[ContainerManagerModel.SlotCount];

        [SerializeField] DebrisPiece debrisPrefab;

        // row position is authored on the ContainerRow prefab root
        public void Init(GameSession session)
        {
            _session = session;
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = ContainerSlotView.Create(transform, new Vector3(SlotX[i], 0f, 0f), debrisPrefab);

            session.Containers.ContainerEntered += OnEntered;
            session.Containers.ContainerCompleted += OnCompleted;
        }

        void OnDestroy()
        {
            if (_session == null) return;
            _session.Containers.ContainerEntered -= OnEntered;
            _session.Containers.ContainerCompleted -= OnCompleted;
        }

        void OnEntered(int slot, ContainerModel model)
        {
            bool afterComplete = _justCompleted[slot];
            _justCompleted[slot] = false;

            Transform pill = null;
            if (model != null && _queuePills.TryGetValue(model, out pill))
                _queuePills.Remove(model);

            if (afterComplete && pill != null)
            {
                // the front queue pill itself is promoted: it flies up into the
                // freed slot while the rest of the column slides up in lockstep
                // (no delay), so the stack never shows a gap under the slot
                _slots[slot].PrepareContainer(model);
                FlyPillIntoSlot(pill, slot);
                SyncQueueDisplay(true, 0f);
            }
            else
            {
                // initial fill (or queue ran out): pop-in, no flight
                if (pill != null) Destroy(pill.gameObject);
                float delay = afterComplete ? GhostCloseTime : 0f;
                _slots[slot].SetContainer(model, delay);
                SyncQueueDisplay(afterComplete, delay);
            }
        }

        void OnCompleted(int slot, ContainerModel model)
        {
            _slots[slot].PlayCompleteGhost();
            _justCompleted[slot] = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayComplete();
        }

        void FlyPillIntoSlot(Transform pill, int slot)
        {
            pill.name = "PromotingPill";
            pill.GetComponent<Renderer>().sortingOrder = 66; // above active pills while in flight
            pill.DOKill();

            var slotView = _slots[slot];
            var seq = DOTween.Sequence();
            seq.Append(pill.DOLocalMove(new Vector3(SlotX[slot], 0f, 0.1f), PillFlyTime)
                .SetEase(Ease.OutBack, 1.2f));
            seq.OnComplete(() =>
            {
                if (pill != null) Destroy(pill.gameObject);
                if (slotView != null) slotView.RevealArrived();
            });
        }

        /// Round-robin the global queue under the four columns so it reads
        /// like the reference's per-column stacks. Pills persist per container
        /// and slide to their new spot instead of being rebuilt in place.
        void SyncQueueDisplay(bool animate, float delay = 0f)
        {
            var alive = new HashSet<ContainerModel>();
            // each column is its own stack: item queued under column c slides up
            // into slot c when that slot completes (no cross-column jumps)
            for (int col = 0; col < SlotX.Length; col++)
            {
                var column = _session.Containers.ColumnSnapshot(col);
                for (int row = 0; row < column.Length && row < QueueRows; row++)
                {
                    var model = column[row];
                    alive.Add(model);
                    var target = new Vector3(SlotX[col], -(row + 1) * RowStep, 0.1f);

                    if (_queuePills.TryGetValue(model, out var pill))
                    {
                        pill.DOKill();
                        if (animate) pill.DOLocalMove(target, QueueShiftTime).SetEase(Ease.OutQuad).SetDelay(delay);
                        else pill.localPosition = target;
                    }
                    else
                    {
                        pill = CreatePillSprite(transform, Palette.Of(model.Color), 61).transform;
                        pill.name = "QueuePill";
                        pill.localRotation = Quaternion.Euler(PillTilt);
                        _queuePills[model] = pill;
                        if (animate)
                        {
                            // shifted into the visible window: slide up from one
                            // row below with a small pop
                            pill.localPosition = target + new Vector3(0f, -RowStep, 0f);
                            pill.localScale = Vector3.zero;
                            pill.DOLocalMove(target, QueueShiftTime).SetEase(Ease.OutQuad).SetDelay(delay);
                            pill.DOScale(1f, QueueShiftTime).SetEase(Ease.OutBack).SetDelay(delay);
                        }
                        else pill.localPosition = target;
                    }
                }
            }

            // containers no longer in the queue (promoted pills were already
            // removed from the map by OnEntered)
            _staleScratch.Clear();
            foreach (var kv in _queuePills)
                if (!alive.Contains(kv.Key)) _staleScratch.Add(kv.Key);
            foreach (var m in _staleScratch)
            {
                if (_queuePills[m] != null) Destroy(_queuePills[m].gameObject);
                _queuePills.Remove(m);
            }
        }

        internal static GameObject CreatePillSprite(Transform parent, Color color, int order)
        {
            var go = new GameObject("Pill", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = CubeMeshFactory.PillMesh();
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterials = CubeMeshFactory.PillMaterials(color);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sortingOrder = order;
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
        MeshRenderer _pill;
        DebrisPiece _debrisPrefab;
        readonly List<GameObject> _sockets = new List<GameObject>();
        readonly List<MeshRenderer> _fillBalls = new List<MeshRenderer>();

        /// World position of one hole, so each ball flies to its own socket
        /// instead of piling onto the container's center.
        public Vector3 SocketPoint(int socketIndex)
        {
            int cap = _model != null ? _model.Capacity : 3;
            if (socketIndex < 0) socketIndex = 0;
            float x = (socketIndex - (cap - 1) * 0.5f) * SocketSpacing;
            return transform.position + new Vector3(x, 0.03f, -0.28f);
        }

        public static ContainerSlotView Create(Transform parent, Vector3 localPos, DebrisPiece debrisPrefab)
        {
            var go = new GameObject("ContainerSlot");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var view = go.AddComponent<ContainerSlotView>();
            view._debrisPrefab = debrisPrefab;
            view._visual = new GameObject("Visual");
            view._visual.transform.SetParent(go.transform, false);
            // tilt the whole container (pill + sockets + fill balls) so the 3D
            // loaf mesh shows its thickness
            view._visual.transform.localRotation = Quaternion.Euler(ContainerRowView.PillTilt);

            var pillGo = ContainerRowView.CreatePillSprite(view._visual.transform, Color.white, 62);
            view._pill = pillGo.GetComponent<MeshRenderer>();

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

            ApplyModelVisual(model);
            _visual.transform.DOKill();
            _visual.transform.localScale = Vector3.zero; // stays invisible during the delay
            _visual.transform.DOScale(1f, 0.32f).SetEase(Ease.OutBack).SetDelay(appearDelay + 0.02f);
        }

        /// Same as SetContainer, but the visual stays hidden — the promoted
        /// queue pill is flying here and RevealArrived does the swap.
        public void PrepareContainer(ContainerModel model)
        {
            _model = model;
            _visual.SetActive(false);
            ApplyModelVisual(model);
        }

        /// The promoted pill just landed: swap it for the real socketed
        /// container with a soft settle instead of a from-zero pop.
        public void RevealArrived()
        {
            if (_model == null) return;
            RefreshDots(); // catch balls that landed while the pill was in flight
            _visual.SetActive(true);
            _visual.transform.DOKill();
            _visual.transform.localScale = Vector3.one * 0.92f;
            _visual.transform.DOScale(1f, 0.18f).SetEase(Ease.OutBack);
        }

        void ApplyModelVisual(ContainerModel model)
        {
            _pill.sharedMaterials = CubeMeshFactory.PillMaterials(Palette.Of(model.Color));
            RebuildSockets(model.Capacity);
            RefreshDots();
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
                socket.transform.localPosition = new Vector3(x, 0f, -0.3f);
                socket.transform.localScale = Vector3.one * 0.88f;
                var sr = socket.GetComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.Socket();
                sr.sortingOrder = 63;
                _sockets.Add(socket);

                var ballGo = new GameObject("FillBall", typeof(MeshFilter), typeof(MeshRenderer));
                ballGo.transform.SetParent(_visual.transform, false);
                // seated deeper in the socket (less negative z = further into the
                // pill, away from the camera) so the ball nests in the hole
                // instead of poking out in front of it
                ballGo.transform.localPosition = new Vector3(x, 0.0f, -0.28f);
                ballGo.transform.localScale = Vector3.one * 0.33f;
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

        /// On completion the filled container snaps shut (a squash, like a lid
        /// closing), rebounds, hops out of its slot and pops away into a shard
        /// burst, while the real slot is immediately reused for the next
        /// container.
        public void PlayCompleteGhost()
        {
            // the completing ball fires this before the view refreshes, so make
            // sure all holes show filled before we clone the box to fly away
            RefreshDots();
            var color = _model != null ? _model.Color : GameColor.Red; // _model is swapped right after this event
            var ghost = Instantiate(_visual, _visual.transform.position, _visual.transform.rotation);
            ghost.name = "Ghost";
            ghost.SetActive(true);
            ghost.transform.localScale = Vector3.one;
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.sortingOrder += 10;

            var t = ghost.transform;
            // launch straight up and out of the container area so it never
            // lingers over the freed slot or the waiting queue below it
            var popPos = t.position + new Vector3(0f, 1.6f, 0f);
            var seq = DOTween.Sequence();
            seq.Append(t.DOScale(new Vector3(1.16f, 0.66f, 1f), 0.09f).SetEase(Ease.OutQuad)); // lid slams shut
            seq.Append(t.DOScale(new Vector3(0.94f, 1.1f, 1f), 0.08f).SetEase(Ease.OutQuad));  // rebound stretch
            seq.Append(t.DOScale(Vector3.one, 0.06f));
            seq.Append(t.DOMoveY(popPos.y, 0.34f).SetEase(Ease.OutCubic));                     // fly up and away...
            seq.Join(t.DOScale(Vector3.zero, 0.34f).SetEase(Ease.InBack));                     // ...shrinking to a pop
            seq.OnComplete(() =>
            {
                if (ghost != null) Destroy(ghost);
                if (this != null) DebrisBurst.Spawn(_debrisPrefab, transform, popPos, color);
            });
            Destroy(ghost, 2f); // safety net if the tween is killed (OnComplete handles the normal case)
        }
    }
}
