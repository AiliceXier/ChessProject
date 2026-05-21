#!/bin/bash
BASE_URL="http://localhost:3000"
PASS=0
FAIL=0
TESTS=()

pass() { PASS=$((PASS+1)); TESTS+=("✅ PASS: $1"); }
fail() { FAIL=$((FAIL+1)); TESTS+=("❌ FAIL: $1 — $2"); }

echo "═══════════════════════════════════════════"
echo "  积分榜 API 自动化测试"
echo "═══════════════════════════════════════════"
echo ""

# ── 0. Cleanup: remove test data ──────────────────────────────
curl -s -X DELETE "$BASE_URL/score/test_player_1" -H "x-admin-key: leaderboard2024" > /dev/null 2>&1
curl -s -X DELETE "$BASE_URL/score/test_player_2" -H "x-admin-key: leaderboard2024" > /dev/null 2>&1
curl -s -X DELETE "$BASE_URL/score/test_admin" -H "x-admin-key: leaderboard2024" > /dev/null 2>&1

# ── 基础测试 ──────────────────────────────────────────────────

echo "【基础测试】"

# 1. GET /ping
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/ping")
if [ "$STATUS" = "200" ]; then pass "GET /ping 返回 200"; else fail "GET /ping 返回 200" "got $STATUS"; fi

# 2. POST /score 正常提交
RESP=$(curl -s -X POST "$BASE_URL/score" \
  -H "Content-Type: application/json" \
  -d '{"player_name":"test_player_1","score":100,"game_mode":"test"}')
SUCCESS=$(echo "$RESP" | grep -o '"success":true')
RANK=$(echo "$RESP" | grep -o '"rank":[0-9]*' | grep -o '[0-9]*')
if [ -n "$SUCCESS" ] && [ -n "$RANK" ]; then
  pass "POST /score 提交正常数据成功 (rank=$RANK)"
else
  fail "POST /score 提交正常数据成功" "response: $RESP"
fi

# 3. GET /leaderboard 返回数组且按分数降序
RESP=$(curl -s "$BASE_URL/leaderboard?game_mode=test")
SCORES=$(echo "$RESP" | grep -o '"score":[0-9]*' | grep -o '[0-9]*' | tr '\n' ' ')
if [ -n "$SCORES" ]; then
  PREV=999999
  OK=1
  for s in $SCORES; do
    if [ $s -gt $PREV ]; then OK=0; fi
    PREV=$s
  done
  if [ $OK -eq 1 ]; then pass "GET /leaderboard 返回数组且按分数降序排列"; else fail "GET /leaderboard 返回数组且按分数降序排列" "not sorted desc"; fi
else
  fail "GET /leaderboard 返回数组且按分数降序排列" "no scores found"
fi

# 4. GET /rank/:player_name 能找到刚提交的玩家
RESP=$(curl -s "$BASE_URL/rank/test_player_1?game_mode=test")
HAS_RANK=$(echo "$RESP" | grep -o '"rank":[0-9]*')
if [ -n "$HAS_RANK" ]; then pass "GET /rank/:player_name 能找到刚提交的玩家"; else fail "GET /rank/:player_name 能找到刚提交的玩家" "response: $RESP"; fi

echo ""

# ── 边界测试 ──────────────────────────────────────────────────

echo "【边界测试】"

# 5. 空 player_name 返回 400
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/score" \
  -H "Content-Type: application/json" \
  -d '{"player_name":"","score":100}')
if [ "$STATUS" = "400" ]; then pass "POST /score 空 player_name 返回 400"; else fail "POST /score 空 player_name 返回 400" "got $STATUS"; fi

# 6. 负数 score 返回 400
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE_URL/score" \
  -H "Content-Type: application/json" \
  -d '{"player_name":"test_neg","score":-5}')
if [ "$STATUS" = "400" ]; then pass "POST /score 负数 score 返回 400"; else fail "POST /score 负数 score 返回 400" "got $STATUS"; fi

# 7. 同一玩家提交更低分，数据库分数不变
curl -s -X POST "$BASE_URL/score" -H "Content-Type: application/json" \
  -d '{"player_name":"test_player_1","score":100,"game_mode":"test"}' > /dev/null
RESP=$(curl -s -X POST "$BASE_URL/score" \
  -H "Content-Type: application/json" \
  -d '{"player_name":"test_player_1","score":50,"game_mode":"test"}')
OLD_SCORE=$(curl -s "$BASE_URL/rank/test_player_1?game_mode=test" | grep -o '"score":[0-9]*' | grep -o '[0-9]*')
if [ "$OLD_SCORE" = "100" ]; then pass "同一玩家提交更低分，分数不变"; else fail "同一玩家提交更低分，分数不变" "got score=$OLD_SCORE"; fi

# 8. 同一玩家提交更高分，数据库分数更新
curl -s -X POST "$BASE_URL/score" -H "Content-Type: application/json" \
  -d '{"player_name":"test_player_1","score":200,"game_mode":"test"}' > /dev/null
NEW_SCORE=$(curl -s "$BASE_URL/rank/test_player_1?game_mode=test" | grep -o '"score":[0-9]*' | grep -o '[0-9]*')
if [ "$NEW_SCORE" = "200" ]; then pass "同一玩家提交更高分，分数更新"; else fail "同一玩家提交更高分，分数更新" "got score=$NEW_SCORE"; fi

# 9. 不存在的玩家返回 success:false
HAS_FALSE=$(curl -s "$BASE_URL/rank/no_one_exists_xyz?game_mode=test" | grep -o '"success":false')
if [ -n "$HAS_FALSE" ]; then pass "GET /rank/不存在的玩家 返回 success:false"; else fail "GET /rank/不存在的玩家 返回 success:false" "response: $RESP"; fi

echo ""

# ── 压力测试 ──────────────────────────────────────────────────

echo "【压力测试】"

# 10. 循环提交50条不同玩家数据
INSERT_OK=1
for i in $(seq 1 50); do
  RESP=$(curl -s -X POST "$BASE_URL/score" \
    -H "Content-Type: application/json" \
    -d "{\"player_name\":\"stress_$i\",\"score\":$((100 + i * 10)),\"game_mode\":\"stress\"}")
  if ! echo "$RESP" | grep -q '"success":true'; then INSERT_OK=0; fi
done
if [ $INSERT_OK -eq 1 ]; then pass "压力测试: 循环提交50条数据成功"; else fail "压力测试: 循环提交50条数据成功" "some inserts failed"; fi

# 11. 排行榜 Top10 排序正确，按分数降序
RESP=$(curl -s "$BASE_URL/leaderboard?limit=10&game_mode=stress")
TOP10=$(echo "$RESP" | grep -o '"score":[0-9]*' | grep -o '[0-9]*' | tr '\n' ' ')
PREV=9999
SORT_OK=1
for s in $TOP10; do
  if [ $s -gt $PREV ]; then SORT_OK=0; fi
  PREV=$s
done
if [ $SORT_OK -eq 1 ]; then pass "压力测试: Top10 排序正确"; else fail "压力测试: Top10 排序正确" "not descending"; fi

# 12. 验证第1名是最高分 (stress_50: 600)
RESP=$(curl -s "$BASE_URL/leaderboard?limit=1&game_mode=stress")
TOP_NAME=$(echo "$RESP" | grep -o '"player_name":"[^"]*"' | head -1 | sed 's/"player_name":"\(.*\)"/\1/')
TOP_SCORE=$(echo "$RESP" | grep -o '"score":[0-9]*' | grep -o '[0-9]*' | head -1)
if [ "$TOP_SCORE" = "600" ] && [ "$TOP_NAME" = "stress_50" ]; then
  pass "压力测试: 第1名是最高分 (stress_50=600)"
else
  fail "压力测试: 第1名是最高分" "top=$TOP_NAME score=$TOP_SCORE expected stress_50=600"
fi

# Clean up stress test data
for i in $(seq 1 50); do
  curl -s -X DELETE "$BASE_URL/score/stress_$i" -H "x-admin-key: leaderboard2024" > /dev/null 2>&1
done

echo ""

# ── 安全测试 ──────────────────────────────────────────────────

echo "【安全测试】"

# 13. DELETE /score 不带 admin key 返回 403
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE "$BASE_URL/score/test_player_1")
if [ "$STATUS" = "403" ]; then pass "DELETE /score 不带 admin key 返回 403"; else fail "DELETE /score 不带 admin key 返回 403" "got $STATUS"; fi

# 14. DELETE /score 带正确 admin key 成功删除
RESP=$(curl -s -X DELETE "$BASE_URL/score/test_player_1" -H "x-admin-key: leaderboard2024")
HAS_SUCCESS=$(echo "$RESP" | grep -o '"success":true')
if [ -n "$HAS_SUCCESS" ]; then pass "DELETE /score 带 admin key 成功删除"; else fail "DELETE /score 带 admin key 成功删除" "response: $RESP"; fi

# Clean up remaining test data
curl -s -X DELETE "$BASE_URL/score/test_player_2" -H "x-admin-key: leaderboard2024" > /dev/null 2>&1
curl -s -X DELETE "$BASE_URL/score/test_admin" -H "x-admin-key: leaderboard2024" > /dev/null 2>&1

echo ""
echo "═══════════════════════════════════════════"
echo "              测试结果汇总"
echo "═══════════════════════════════════════════"

for t in "${TESTS[@]}"; do
  echo "$t"
done

echo ""
TOTAL=$((PASS + FAIL))
echo "通过率: $PASS/$TOTAL ($(( PASS * 100 / TOTAL ))%)"
echo ""

if [ $FAIL -gt 0 ]; then
  echo "⚠️  存在失败测试，请检查！"
  exit 1
else
  echo "🎉 全部测试通过！"
  exit 0
fi
