using System;
using System.Collections;
using System.Collections.Generic;
using Chess.Audio;
using UnityEngine;

namespace Chess.Animation
{
    public class MoveAnimator : MonoBehaviour
    {
        public float moveDuration = 0.25f;
        public float captureDuration = 0.2f;
        public float spawnDuration = 0.15f;
        public float arcHeight = 0.5f;
        public GameObject board;

        private bool _animating;

        public bool IsAnimating => _animating;

        public IEnumerator AnimateSyncBoard(
            Dictionary<Tuple<int, int>, char> newBoardState,
            Dictionary<string, UnityEngine.Object> prefabs,
            System.Action<GameObject, char> setupPiece,
            System.Action onComplete = null)
        {
            _animating = true;

            var oldPieces = new Dictionary<string, GameObject>();
            foreach (Transform child in board.transform)
            {
                if (child == null || child.gameObject == null) continue;
                var name = child.gameObject.name;
                if (name == "MoveHighlight" || name == "HintHighlight") continue;
                var pos = child.localPosition;
                int x = Mathf.RoundToInt(pos.x);
                int z = Mathf.RoundToInt(pos.z);
                if (x >= 0 && x < 8 && z >= 0 && z < 8)
                {
                    oldPieces[PosKey(x, z)] = child.gameObject;
                }
            }

            var newPieces = new Dictionary<string, Tuple<int, int, char>>();
            foreach (var kvp in newBoardState)
            {
                var key = PosKey(kvp.Key.Item1, kvp.Key.Item2);
                newPieces[key] = Tuple.Create(kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
            }

            var toCapture = new List<GameObject>();
            var toRemove = new List<string>();

            foreach (var oldKvp in oldPieces)
            {
                if (!newPieces.ContainsKey(oldKvp.Key))
                {
                    // Piece might have moved to a new position - handle in unmatchedOld matching below
                }
                else
                {
                    var oldPiece = oldKvp.Value;
                    if (oldPiece == null) continue;
                    var oldName = oldPiece.name.Replace("(Clone)", "").Trim();
                    var newChar = newPieces[oldKvp.Key].Item3;
                    var newName = GetPieceTypeName(newChar) + (char.IsUpper(newChar) ? "Light" : "Dark");

                    if (oldName != newName)
                    {
                        toCapture.Add(oldPiece);
                        toRemove.Add(oldKvp.Key);
                    }
                }
            }

            foreach (var key in toRemove)
                oldPieces.Remove(key);

            foreach (var captured in toCapture)
            {
                if (captured != null)
                    yield return StartCoroutine(AnimateCapture(captured));
            }

            var toMove = new Dictionary<GameObject, Tuple<int, int>>();
            var toAdd = new Dictionary<string, Tuple<int, int, char>>();

            var remainingNew = new Dictionary<string, Tuple<int, int, char>>(newPieces);

            foreach (var oldKvp in oldPieces)
            {
                if (remainingNew.ContainsKey(oldKvp.Key))
                {
                    remainingNew.Remove(oldKvp.Key);
                    continue;
                }
            }

            var unmatchedOld = new List<KeyValuePair<string, GameObject>>();
            foreach (var oldKvp in oldPieces)
            {
                if (newPieces.ContainsKey(oldKvp.Key))
                    continue;

                unmatchedOld.Add(oldKvp);
            }

            foreach (var oldKvp in unmatchedOld)
            {
                var oldPiece = oldKvp.Value;
                if (oldPiece == null) continue;
                var oldName = oldPiece.name.Replace("(Clone)", "").Trim();

                string bestNewKey = null;
                foreach (var nkvp in remainingNew)
                {
                    var newChar = nkvp.Value.Item3;
                    var newName = GetPieceTypeName(newChar) + (char.IsUpper(newChar) ? "Light" : "Dark");
                    if (oldName == newName)
                    {
                        bestNewKey = nkvp.Key;
                        break;
                    }
                }

                if (bestNewKey != null)
                {
                    var target = remainingNew[bestNewKey];
                    toMove[oldPiece] = Tuple.Create(target.Item1, target.Item2);
                    remainingNew.Remove(bestNewKey);
                }
                else
                {
                    if (oldPiece != null)
                        yield return StartCoroutine(AnimateCapture(oldPiece));
                }
            }

            foreach (var kvp in toMove)
            {
                var piece = kvp.Key;
                var target = kvp.Value;
                yield return StartCoroutine(AnimateMove(piece, target.Item1, target.Item2));
            }

            foreach (var kvp in remainingNew)
            {
                var x = kvp.Value.Item1;
                var z = kvp.Value.Item2;
                var c = kvp.Value.Item3;

                var pieceType = GetPieceTypeName(c);
                var prefabName = pieceType + (char.IsUpper(c) ? "Light" : "Dark");
                if (!prefabs.ContainsKey(prefabName))
                    prefabs[prefabName] = Resources.Load($"{pieceType}/Prefabs/{prefabName}");

                var newObject = Instantiate(prefabs[prefabName], board.transform);
                var go = newObject as GameObject;
                if (go == null) continue;
                go.transform.localPosition = new Vector3(x, 0, z);
                go.transform.localRotation = Quaternion.Euler(0, char.IsLower(c) ? 180 : 0, 0);
                setupPiece?.Invoke(go, c);

                yield return StartCoroutine(AnimateSpawn(go));
            }

            _animating = false;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateMove(GameObject piece, int targetX, int targetZ)
        {
            if (piece == null) yield break;

            var startPos = piece.transform.localPosition;
            var endPos = new Vector3(targetX, 0, targetZ);
            var elapsed = 0f;

            while (elapsed < moveDuration)
            {
                if (piece == null) yield break;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / moveDuration);
                var smoothT = t * t * (3f - 2f * t);

                var pos = Vector3.Lerp(startPos, endPos, smoothT);
                pos.y = arcHeight * Mathf.Sin(smoothT * Mathf.PI);
                piece.transform.localPosition = pos;

                yield return null;
            }

            if (piece != null)
            {
                piece.transform.localPosition = endPos;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayPieceMove();
            }
        }

        private IEnumerator AnimateCapture(GameObject piece)
        {
            if (piece == null) yield break;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCapture();

            var elapsed = 0f;
            var startScale = piece.transform.localScale;

            while (elapsed < captureDuration)
            {
                if (piece == null) yield break;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / captureDuration);
                piece.transform.localScale = startScale * (1f - t);
                yield return null;
            }

            if (piece != null)
                Destroy(piece);
        }

        private IEnumerator AnimateSpawn(GameObject piece)
        {
            var elapsed = 0f;
            var targetScale = piece.transform.localScale;
            piece.transform.localScale = Vector3.zero;

            while (elapsed < spawnDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / spawnDuration);
                var smoothT = t * t;
                piece.transform.localScale = targetScale * smoothT;
                yield return null;
            }

            piece.transform.localScale = targetScale;
        }

        private static string PosKey(int x, int z) => $"{x},{z}";

        private static string GetPieceTypeName(char c)
        {
            return char.ToLower(c) switch
            {
                'p' => "Pawn",
                'n' => "Knight",
                'b' => "Bishop",
                'r' => "Rook",
                'q' => "Queen",
                'k' => "King",
                _ => ""
            };
        }
    }
}
