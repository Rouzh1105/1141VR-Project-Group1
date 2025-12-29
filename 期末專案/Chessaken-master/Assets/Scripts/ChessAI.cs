using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChessAI : MonoBehaviour
{
    public Board board;
    public int aiTeam = 1; // 黑棋
    public float thinkDelay = 0.5f;
    public float moveDelay = 1f;

    Dictionary<string, int> pieceValue = new Dictionary<string, int>()
    {
        { "Pawn", 10 },
        { "Horse", 30 },
        { "Bishop", 30 },
        { "Tower", 50 },
        { "Queen", 90 },
        { "King", 1000 }
    };

    public void PlayOneMove()
    {
        StartCoroutine(AIMoveCoroutine());
    }

    IEnumerator AIMoveCoroutine()
    {
        yield return new WaitForSeconds(thinkDelay);

        // 快照，避免 Destroy / 時序問題
        Piece[] allPieces = FindObjectsOfType<Piece>();
        Square[] allSquares = FindObjectsOfType<Square>();

        float bestScore = float.NegativeInfinity;
        Piece bestPiece = null;
        Square bestSquare = null;

        bool foundAnyMove = false;

        for (int i = 0; i < allPieces.Length; i++)
        {
            Piece p = allPieces[i];
            if (!IsAlivePiece(p)) continue;
            if (p.team != aiTeam) continue;

            board.addPieceBreakPoints(p);

            for (int j = 0; j < allSquares.Length; j++)
            {
                Square s = allSquares[j];
                if (s == null) continue;

                if (!p.checkValidMove(s)) continue;

                // ❌ 永遠禁止吃王（避免高分但非法）
                if (s.holding_piece != null &&
                    s.holding_piece.piece_name == "King")
                    continue;

                foundAnyMove = true;

                float score = EvaluateMove(p, s, allPieces);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPiece = p;
                    bestSquare = s;
                }
            }
        }

        // === 完全沒有合法走法（將死 / 僵局）===
        if (!foundAnyMove)
        {
            Debug.Log("AI No Legal Move (Checkmate or Stalemate)");
            board.changeTurn(); // 保底，避免卡回合
            yield break;
        }

        // === 嘗試實際走棋 ===
        if (IsAlivePiece(bestPiece) && bestSquare != null)
        {
            Square before = bestPiece.cur_square;

            bestPiece.movePiece(bestSquare);

            // ★ 關鍵保底：如果 movePiece 被內部拒絕
            if (bestPiece.cur_square == before)
            {
                Debug.LogWarning(
                    $"AI move rejected internally (score={bestScore}), forcing fallback");

                // fallback：找任一可動步直接走
                bool moved = false;

                for (int i = 0; i < allPieces.Length && !moved; i++)
                {
                    Piece p = allPieces[i];
                    if (!IsAlivePiece(p)) continue;
                    if (p.team != aiTeam) continue;

                    board.addPieceBreakPoints(p);

                    for (int j = 0; j < allSquares.Length; j++)
                    {
                        Square s = allSquares[j];
                        if (s == null) continue;
                        if (!p.checkValidMove(s)) continue;

                        // 同樣避免吃王
                        if (s.holding_piece != null &&
                            s.holding_piece.piece_name == "King")
                            continue;

                        Square b = p.cur_square;
                        p.movePiece(s);

                        if (p.cur_square != b)
                        {
                            moved = true;
                            Debug.Log("AI fallback move executed");
                            break;
                        }
                    }
                }

                if (!moved)
                {
                    Debug.LogWarning("AI fallback failed, forcing turn change");
                    board.changeTurn();
                }

                yield break;
            }

            Debug.Log($"AI Move (score = {bestScore})");
        }
        else
        {
            Debug.LogWarning("AI best move invalid, forcing turn change");
            board.doCheckMate(board.cur_turn);
        }

        yield return new WaitForSeconds(moveDelay);
    }

    // =========================
    // 評分函式（加上防呆）
    // =========================
    float EvaluateMove(Piece piece, Square target, Piece[] allPiecesSnapshot)
    {
        if (!IsAlivePiece(piece) || target == null) return float.NegativeInfinity;

        float score = 0f;

        // 1) 吃子加分（TryGetValue 防止 KeyNotFound）
        Piece targetPiece = target.holding_piece;
        if (IsAlivePiece(targetPiece))
        {
            score += GetPieceValSafe(targetPiece.piece_name);
        }

        // ===== 模擬走棋（用 holding_piece，不要用已吃的那顆）=====
        Square from = piece.cur_square;
        if (from == null) return float.NegativeInfinity;

        Piece captured = target.holding_piece; // 可能為 null

        // 暫存
        from.holdPiece(null);
        target.holdPiece(piece);
        piece.cur_square = target;

        // 2) 自己被將軍 → 大扣分
        if (board.isCheckKing(aiTeam))
            score -= 1000f;

        // 3) 造成對方將軍 → 加分
        if (board.isCheckKing(-aiTeam))
            score += 50f;

        // 4) 走完會被吃 → 扣自己價值
        for (int i = 0; i < allPiecesSnapshot.Length; i++)
        {
            Piece enemy = allPiecesSnapshot[i];
            if (!IsAlivePiece(enemy)) continue;
            if (enemy.team == aiTeam) continue;

            board.addPieceBreakPoints(enemy);

            // enemy 可能在同幀被 Destroy（極少但會發生），再檢查一次
            if (!IsAlivePiece(enemy)) continue;

            if (enemy.checkValidMove(target))
            {
                score -= GetPieceValSafe(piece.piece_name);
                break; // 被任何一個吃到就夠扣了
            }
        }

        // ===== 還原棋盤 =====
        piece.cur_square = from;
        from.holdPiece(piece);
        target.holdPiece(captured);

        // 5) 微隨機：避免完全固定
        score += Random.Range(0f, 0.2f);

        return score;
    }

    // ------- helpers -------
    bool IsAlivePiece(Piece p)
    {
        // Unity Destroy 後的物件會在比較時變成 null
        if (p == null) return false;
        if (p.gameObject == null) return false;
        if (!p.gameObject.activeInHierarchy) return false;
        // 有些情況被吃掉但 cur_square 還沒清，先用 active 來擋大多數
        return true;
    }

    int GetPieceValSafe(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        return pieceValue.TryGetValue(name, out int v) ? v : 0;
    }
}