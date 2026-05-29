using System;
using System.Collections;
using System.Collections.Generic;
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

        private readonly Dictionary<string, GameObject> _piecesByPos = new();
        private bool _animating;

        public bool IsAnimating => _animating;

        public void ClearAll()
        {
            _piecesByPos.Clear();
        }

        private void RebuildPiecesMap()
        {
            _piecesByPos.Clear();
            if (board == null) return;

            foreach (Transform child in board.transform)
            {
                var pos = child.localPosition;
                int x = Mathf.RoundToInt(pos.x);
                int z = Mathf.RoundToInt(pos.z);
                if (x >= 0 && x < 8 && z >= 0 && z < 8)
                {
                    var key = PosKey(x, z);
                    _piecesByPos[key] = child.gameObject;
                }
            }
        }

        public IEnumerator AnimateSyncBoard(
            Dictionary<Tuple<int, int>, char> newBoardState,
            Dictionary<string, UnityEngine.Object> prefabs,
            System.Action<GameObject, char> setupPiece)
        {
            _animating = true;

            RebuildPiecesMap();

            var newPieces = new Dictionary<string, Tuple<int, int, char>>();
            foreach (var kvp in newBoardState)
            {
                var key = PosKey(kvp.Key.Item1, kvp.Key.Item2);
                newPieces[key] = Tuple.Create(kvp.Key.Item1, kvp.Key.Item2, kvp.Value);
            }

            var capturedPieces = new List<GameObject>();
            var toRemove = new List<string>();

            foreach (var existingKey in _piecesByPos.Keys)
            {
                if (!newPieces.ContainsKey(existingKey))
                    toRemove.Add(existingKey);
            }

            foreach (var removeKey in toRemove)
            {
                var piece = _piecesByPos[removeKey];
                _piecesByPos.Remove(removeKey);
                capturedPieces.Add(piece);
            }

            foreach (var captured in capturedPieces)
            {
                if (captured != null)
                    yield return StartCoroutine(AnimateCapture(captured));
            }

            var matchedOld = new HashSet<string>();
            var remainingNew = new Dictionary<string, Tuple<int, int, char>>(newPieces);

            foreach (var oldKey in _piecesByPos.Keys)
            {
                if (newPieces.ContainsKey(oldKey))
                {
                    var oldPiece = _piecesByPos[oldKey];
                    if (oldPiece == null) continue;

                    var oldName = oldPiece.name.Replace("(Clone)", "").Trim();
                    var newChar = newPieces[oldKey].Item3;
                    var newName = GetPieceTypeName(newChar) + (char.IsUpper(newChar) ? "Light" : "Dark");

                    if (oldName == newName)
                    {
                        matchedOld.Add(oldKey);
                        remainingNew.Remove(oldKey);
                    }
                }
            }

            var moveAnimations = new List<Coroutine>();

            var oldKeysCopy = new List<string>(_piecesByPos.Keys);
            foreach (var oldKey in oldKeysCopy)
            {
                if (matchedOld.Contains(oldKey)) continue;
                if (!_piecesByPos.TryGetValue(oldKey, out var oldPiece) || oldPiece == null) continue;

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
                    var target = newPieces[bestNewKey];
                    _piecesByPos.Remove(oldKey);
                    _piecesByPos[bestNewKey] = oldPiece;
                    remainingNew.Remove(bestNewKey);

                    moveAnimations.Add(StartCoroutine(AnimateMove(oldPiece, target.Item1, target.Item2)));
                }
            }

            foreach (var cor in moveAnimations)
                yield return cor;

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

                _piecesByPos[kvp.Key] = go;

                yield return StartCoroutine(AnimateSpawn(go));
            }

            _animating = false;
        }

        private IEnumerator AnimateMove(GameObject piece, int targetX, int targetZ)
        {
            var startPos = piece.transform.localPosition;
            var endPos = new Vector3(targetX, 0, targetZ);
            var elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / moveDuration);
                var smoothT = t * t * (3f - 2f * t);

                var pos = Vector3.Lerp(startPos, endPos, smoothT);
                pos.y = arcHeight * Mathf.Sin(smoothT * Mathf.PI);
                piece.transform.localPosition = pos;

                yield return null;
            }

            piece.transform.localPosition = endPos;
        }

        private IEnumerator AnimateCapture(GameObject piece)
        {
            var elapsed = 0f;
            var startScale = piece.transform.localScale;

            while (elapsed < captureDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / captureDuration);
                piece.transform.localScale = startScale * (1f - t);
                yield return null;
            }

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
