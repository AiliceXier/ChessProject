using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Audio;

namespace Chess.Animation
{
    public class GameEndAnimator : MonoBehaviour
    {
        public GameObject board;
        public GameObject cameraPivot;

        private bool _animating;

        public bool IsAnimating => _animating;

        /// <summary>
        /// 国王加冕动画：上升、变金色、旋转、恢复
        /// </summary>
        public IEnumerator AnimateKingCoronation(GameObject king)
        {
            if (king == null) yield break;

            var startPos = king.transform.localPosition;
            var renderer = king.GetComponent<Renderer>();
            var originalColor = renderer != null ? renderer.material.color : Color.white;
            var goldColor = new Color(1f, 0.84f, 0f);

            // 1. 缓慢上升1.5个单位（0.6秒）
            var riseTarget = startPos + Vector3.up * 1.5f;
            yield return StartCoroutine(AnimateLocalPosition(king, startPos, riseTarget, 0.6f));

            // 2. 材质颜色变为金色（0.3秒）
            if (renderer != null)
                yield return StartCoroutine(AnimateColor(renderer, originalColor, goldColor, 0.3f));

            // 3. 绕Y轴旋转360度（0.6秒）
            yield return StartCoroutine(AnimateRotationY(king, 0f, 360f, 0.6f));

            // 4. 材质颜色恢复原色，落回原位（0.6秒）
            if (renderer != null)
                yield return StartCoroutine(AnimateColor(renderer, goldColor, originalColor, 0.6f));
            yield return StartCoroutine(AnimateLocalPosition(king, king.transform.localPosition, startPos, 0.6f));
        }

        /// <summary>
        /// 棋子弹跳动画
        /// </summary>
        public IEnumerator AnimatePieceBounce(GameObject piece, float delay)
        {
            if (piece == null) yield break;

            yield return new WaitForSeconds(delay);

            var startPos = piece.transform.localPosition;
            var peakPos = startPos + Vector3.up * 0.5f;
            var elapsed = 0f;
            var duration = 0.4f;

            while (elapsed < duration)
            {
                if (piece == null) yield break;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var smoothT = t * t * (3f - 2f * t);

                // 先上升后下降，使用正弦曲线
                var height = Mathf.Sin(smoothT * Mathf.PI) * 0.5f;
                piece.transform.localPosition = startPos + Vector3.up * height;

                yield return null;
            }

            if (piece != null)
                piece.transform.localPosition = startPos;
        }

        /// <summary>
        /// 棋子倾倒动画
        /// </summary>
        public IEnumerator AnimatePieceFall(GameObject piece, float delay)
        {
            if (piece == null) yield break;

            yield return new WaitForSeconds(delay);

            var isWhite = piece.name.Contains("Light");
            // 白方棋子向Z正方向倒，黑方向Z负方向倒
            var fallDirection = isWhite ? Vector3.forward : Vector3.back;
            var targetRotation = Quaternion.Euler(fallDirection == Vector3.forward ? new Vector3(90f, 0f, 0f) : new Vector3(-90f, 0f, 0f));
            var startRotation = piece.transform.localRotation;
            var startScale = piece.transform.localScale;
            var targetScale = startScale * 0.3f;
            var elapsed = 0f;
            var duration = 0.5f;

            while (elapsed < duration)
            {
                if (piece == null) yield break;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var smoothT = t * t * (3f - 2f * t);

                piece.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, smoothT);
                piece.transform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

                yield return null;
            }

            if (piece != null)
            {
                piece.transform.localRotation = targetRotation;
                piece.transform.localScale = targetScale;
            }
        }

        /// <summary>
        /// 胜利动画组合
        /// </summary>
        public IEnumerator PlayWinAnimation(PieceColor wonSide, System.Action onComplete)
        {
            _animating = true;

            // 播放胜利音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayWin();

            // 同时启动相机俯仰动画
            var cameraCoroutine = cameraPivot != null ? StartCoroutine(AnimateCameraTilt(-40f, 1.5f)) : null;

            var isWhite = wonSide == PieceColor.White;
            var wonKing = FindKing(isWhite);
            var lostKing = FindKing(!isWhite);

            // 赢方国王加冕动画
            if (wonKing != null)
                yield return StartCoroutine(AnimateKingCoronation(wonKing));

            // 赢方其余棋子按距离国王远近依次弹跳
            var wonPieces = FindPiecesBySide(isWhite);
            wonPieces = wonPieces
                .Where(p => p != wonKing)
                .OrderBy(p => DistanceToKing(p, wonKing))
                .ToList();

            var bounceCoroutines = new List<Coroutine>();
            for (int i = 0; i < wonPieces.Count; i++)
            {
                var delay = (i + 1) * 0.1f;
                bounceCoroutines.Add(StartCoroutine(AnimatePieceBounce(wonPieces[i], delay)));
            }

            // 输方棋子按距离输方国王远近依次倾倒
            var lostPieces = FindPiecesBySide(!isWhite);
            lostPieces = lostPieces
                .Where(p => p != lostKing)
                .OrderBy(p => DistanceToKing(p, lostKing))
                .ToList();

            var fallCoroutines = new List<Coroutine>();
            for (int i = 0; i < lostPieces.Count; i++)
            {
                var delay = (i + 1) * 0.08f;
                fallCoroutines.Add(StartCoroutine(AnimatePieceFall(lostPieces[i], delay)));
            }

            // 等待所有动画完成
            foreach (var coroutine in bounceCoroutines)
                yield return coroutine;
            foreach (var coroutine in fallCoroutines)
                yield return coroutine;
            if (cameraCoroutine != null)
                yield return cameraCoroutine;

            _animating = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 和棋动画
        /// </summary>
        public IEnumerator PlayDrawAnimation(System.Action onComplete)
        {
            _animating = true;

            // 播放失败音效
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayLose();

            // 同时启动相机俯仰动画
            var cameraCoroutine = cameraPivot != null ? StartCoroutine(AnimateCameraTilt(-40f, 1.5f)) : null;

            var whiteKing = FindKing(true);
            var blackKing = FindKing(false);

            // 双方国王同时动画
            var whiteKingCoroutine = whiteKing != null ? StartCoroutine(AnimateDrawKing(whiteKing)) : null;
            var blackKingCoroutine = blackKing != null ? StartCoroutine(AnimateDrawKing(blackKing)) : null;

            if (whiteKingCoroutine != null) yield return whiteKingCoroutine;
            if (blackKingCoroutine != null) yield return blackKingCoroutine;
            if (cameraCoroutine != null) yield return cameraCoroutine;

            _animating = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 和棋时国王动画：上升、摇晃、落回
        /// </summary>
        private IEnumerator AnimateDrawKing(GameObject king)
        {
            if (king == null) yield break;

            var startPos = king.transform.localPosition;
            var risePos = startPos + Vector3.up * 0.8f;

            // 上升0.8单位（0.4秒）
            yield return StartCoroutine(AnimateLocalPosition(king, startPos, risePos, 0.4f));

            // 左右轻微摇晃（0.6秒）
            var shakeDuration = 0.6f;
            var shakeElapsed = 0f;
            var shakeAmplitude = 0.15f;
            while (shakeElapsed < shakeDuration)
            {
                if (king == null) yield break;
                shakeElapsed += Time.deltaTime;
                var t = shakeElapsed / shakeDuration;
                var xOffset = Mathf.Sin(t * Mathf.PI * 4f) * shakeAmplitude * (1f - t);
                king.transform.localPosition = risePos + Vector3.right * xOffset;
                yield return null;
            }

            if (king != null)
                king.transform.localPosition = risePos;

            // 落回原位（0.4秒）
            yield return StartCoroutine(AnimateLocalPosition(king, king.transform.localPosition, startPos, 0.4f));
        }

        /// <summary>
        /// 平滑移动位置
        /// </summary>
        private IEnumerator AnimateLocalPosition(GameObject obj, Vector3 from, Vector3 to, float duration)
        {
            if (obj == null) yield break;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var smoothT = t * t * (3f - 2f * t);
                obj.transform.localPosition = Vector3.Lerp(from, to, smoothT);
                yield return null;
            }

            if (obj != null)
                obj.transform.localPosition = to;
        }

        /// <summary>
        /// 平滑颜色过渡
        /// </summary>
        private IEnumerator AnimateColor(Renderer renderer, Color from, Color to, float duration)
        {
            if (renderer == null) yield break;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var smoothT = t * t * (3f - 2f * t);
                renderer.material.color = Color.Lerp(from, to, smoothT);
                yield return null;
            }

            renderer.material.color = to;
        }

        /// <summary>
        /// 绕Y轴旋转
        /// </summary>
        private IEnumerator AnimateRotationY(GameObject obj, float fromAngle, float toAngle, float duration)
        {
            if (obj == null) yield break;

            var startRot = obj.transform.localEulerAngles;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (obj == null) yield break;
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var smoothT = t * t * (3f - 2f * t);
                var angle = Mathf.Lerp(fromAngle, toAngle, smoothT);
                obj.transform.localEulerAngles = new Vector3(startRot.x, startRot.y + angle, startRot.z);
                yield return null;
            }

            if (obj != null)
                obj.transform.localEulerAngles = new Vector3(startRot.x, startRot.y + toAngle, startRot.z);
        }

        /// <summary>
        /// 通过遍历 board.transform 子对象，根据名字找到对应方棋子
        /// </summary>
        private List<GameObject> FindPiecesBySide(bool isWhite)
        {
            var pieces = new List<GameObject>();
            var searchName = isWhite ? "Light" : "Dark";

            foreach (Transform child in board.transform)
            {
                if (child == null || child.gameObject == null) continue;
                var name = child.gameObject.name;
                if (name == "MoveHighlight" || name == "HintHighlight") continue;
                if (name.Contains(searchName))
                    pieces.Add(child.gameObject);
            }

            return pieces;
        }

        /// <summary>
        /// 找到对应方的国王棋子
        /// </summary>
        private GameObject FindKing(bool isWhite)
        {
            var kingName = isWhite ? "KingLight" : "KingDark";

            foreach (Transform child in board.transform)
            {
                if (child == null || child.gameObject == null) continue;
                if (child.gameObject.name.Contains(kingName))
                    return child.gameObject;
            }

            return null;
        }

        /// <summary>
        /// 计算棋子到国王的距离
        /// </summary>
        private float DistanceToKing(GameObject piece, GameObject king)
        {
            if (piece == null || king == null) return float.MaxValue;
            return Vector3.Distance(piece.transform.localPosition, king.transform.localPosition);
        }

        /// <summary>
        /// 相机俯仰动画：将 CameraPivot 的 X 轴旋转平滑过渡到目标角度
        /// </summary>
        private IEnumerator AnimateCameraTilt(float targetAngleX, float duration)
        {
            if (cameraPivot == null) yield break;

            var startEuler = cameraPivot.transform.eulerAngles;
            var startAngleX = startEuler.x;
            // 处理角度环绕问题
            if (startAngleX > 180f) startAngleX -= 360f;
            var targetX = targetAngleX;
            var y = startEuler.y;
            var z = startEuler.z;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var smoothT = t * t * (3f - 2f * t);
                var currentX = Mathf.Lerp(startAngleX, targetX, smoothT);
                cameraPivot.transform.eulerAngles = new Vector3(currentX, y, z);
                yield return null;
            }

            cameraPivot.transform.eulerAngles = new Vector3(targetX, y, z);
        }

        /// <summary>
        /// 重置所有棋子的缩放和旋转，用于新一局开始前恢复被动画修改的棋子
        /// </summary>
        public void ResetAllPieces()
        {
            if (board == null) return;

            foreach (Transform child in board.transform)
            {
                if (child == null || child.gameObject == null) continue;
                var name = child.gameObject.name;
                if (name == "MoveHighlight" || name == "HintHighlight") continue;
                child.localScale = Vector3.one;
                var isWhite = name.Contains("Light");
                child.localRotation = Quaternion.Euler(0, isWhite ? 0 : 180, 0);
            }

            // 恢复相机角度
            if (cameraPivot != null)
                cameraPivot.transform.eulerAngles = new Vector3(0, cameraPivot.transform.eulerAngles.y, 0);

            _animating = false;
        }
    }
}
