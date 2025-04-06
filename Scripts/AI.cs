using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

// classes 
 public abstract partial class AI : Player
{   
    Board chess_board;
    Piece.PieceColor color;

     bool piece_picked = false; // there is a piece being selected
    public Piece selected_piece = null; // selected piece in play

    public PieceHistory last_history = null;

    public MoveManager valid_moves; // movemanager for valid moves
    Vector2 original_position = Vector2.Zero; // selected piece original position

    string state = ""; // just a text string to set for check/checkmate. Does not interact with game
    
    public enum BotType {
        Random = 0,
        AlphaBeta = 1,
        MonteCarlo = 2,
    }

    public AI(Board chess_board, Piece.PieceColor color) : base(chess_board, color){
        this.chess_board = chess_board;
        this.color = color;
    }

    public abstract void make_move();

    public abstract List<Move> get_all_moves();




    // sets the selected piece given the input
    public void set_selected_piece(Piece p) {
        if (p != null) 
            {   
                // make sure to reset these attributes if unselected
                if (p.get_piece_color() == (int) chess_board.gm.get_current_turn()) 
                {
                    selected_piece = p; 
                    piece_picked = true; // th ere is a piece being selected
                    original_position = p.GlobalPosition;


                    // get the possible moves
                    MoveManager mvm = new MoveManager(p, chess_board);
                    valid_moves = mvm; // to get valid moves
                    mvm.get_cardinal_movement(original_position); // set all cardinal
                    mvm.get_intermediate_movement(original_position);
                    if (p.get_piece_type() == Piece.PieceType.Knight) {
                        mvm.get_knight_movement(original_position);
                    }
                    if (p.get_piece_type() == Piece.PieceType.King) {
                        mvm.get_castle(original_position, p);
                    }
                    if (p.get_piece_type() == Piece.PieceType.Pawn) {
                        mvm.get_en_passant(original_position);
                    }       
                }
            }
        }


    // plays the selected move in the format (col, row)
    // assume that selected_piece is not null
    public void place_selected_piece(Tuple<int, int> move) {
                //GD.Print(get_piece_under_mouse() + "undermouse");
        piece_picked = false; // set the piece selection to none
        bool success = false; // piece invalid until true
        
        // check conditions to make the move if it exists
        // MOVE must exist for move_validation to even run (see chess_board.cs)
        success = valid_moves != null && move != null && valid_moves.get_move_list_strings().Contains(move.ToString());          
        // if the move was valid, make changes on the board! and reset
        if (success) {
            selected_piece.phist = chess_board.make_move(selected_piece, move); // assign phist MOVE IS MADE HERE
            selected_piece.ChangeState(Piece.State.Placed);                    

           last_history = selected_piece.phist;

            // CASTLE LOGIC, 2 tile distance. No need to fix king position 

            if (selected_piece.get_piece_type() == Piece.PieceType.King &&
            Math.Abs(selected_piece.get_vector_position().X - original_position.X) == chess_board.CELL_SIZE * 2 
            && !chess_board.is_checked((Piece.PieceColor)selected_piece.get_piece_color(), chess_board.BoardTiles,
            new Tuple<int, int>((int)original_position.Y / chess_board.CELL_SIZE, (int)original_position.X / chess_board.CELL_SIZE)).Item1)  
            {
                if (Math.Abs(move.Item2 - 7) < Math.Abs(move.Item2) && chess_board.BoardTiles[move.Item1, chess_board.DIMENSION_X - 1] != null
                && chess_board.BoardTiles[move.Item1, chess_board.DIMENSION_X - 1].get_state() == Piece.State.Unmoved) {
                    Piece rookr = chess_board.BoardTiles[move.Item1, chess_board.DIMENSION_X - 1];
                    Tuple<int, int> castle_right = new Tuple<int, int>(move.Item1, move.Item2 - 1);
                    rookr.phist = chess_board.make_move(rookr, castle_right);
                    rookr.ChangeState(Piece.State.Placed);
                }
                else {
                    Piece rookl = chess_board.BoardTiles[move.Item1, 0];
                    Tuple<int, int> castle_left = new Tuple<int, int>(move.Item1, move.Item2 + 1);
                    rookl.phist = chess_board.make_move(rookl, castle_left);
                    rookl.ChangeState(Piece.State.Placed);
                }
            }

            // ENPASSANT LOGIC, no null check since en passant square auto updated, perspective of attacker
            if (chess_board.en_passant_square != null && selected_piece.get_piece_type() == Piece.PieceType.Pawn && 
            chess_board.en_passant_square.ToString().Equals(move.ToString())) {
                Piece passed_pawn = chess_board.BoardTiles[move.Item1 - selected_piece.get_piece_color(), move.Item2];
                passed_pawn.ChangeState(Piece.State.Captured);   
            }
            chess_board.en_passant_square = null; // always set en passant to null, opporunity passes
                // set en passant square from perspective of setter
            if (selected_piece.get_piece_type() == Piece.PieceType.Pawn 
                && Math.Abs(selected_piece.get_vector_position().Y - original_position.Y) == chess_board.CELL_SIZE * 2 ) 
            {
                chess_board.en_passant_square = new Tuple<int, int>(move.Item1 - selected_piece.get_piece_color(), move.Item2);
            }

            // PAWN PROMOTION LOGIC, no color check required since pawns naturally cant go backwards
            if (selected_piece.get_piece_type() == Piece.PieceType.Pawn ) {
                if (move.Item1 == 0 || move.Item1 == chess_board.DIMENSION_Y) {
                    chess_board.gm.promote_pawn(selected_piece);
                    
                }
            } // also remember to check for checks and checkmates after


            // CHECKING LOGIC

            // get the king of opposite color, you are the attacker
            // also get the color of the possible blockers
            Piece king = (selected_piece.get_piece_color() == (int) Piece.PieceColor.White) ? Board.Black_King : Board.White_King;
            Piece.PieceColor col = (selected_piece.get_piece_color() == (int) Piece.PieceColor.White) ? Piece.PieceColor.Black : Piece.PieceColor.White;
            // handle check and checkmate

            state = "";

            // state set here. Perspective of hte attacker
            if (chess_board.is_checked(col, chess_board.BoardTiles).Item1) {
                state = "Check";
                if (chess_board.is_checkmated(col, chess_board.BoardTiles, king.get_threats(king.get_board_position(), chess_board.BoardTiles)[0])) {
                    GameManager.GameState gs = (selected_piece.get_piece_color() == (int)Piece.PieceColor.White) ? GameManager.GameState.White_win : GameManager.GameState.Black_win;
                    chess_board.gm.set_state(gs);
                    state = "Checkmate";
                }
            }
            

            // EVERYTHING HERE MEANS PLAYER SUCCESS
            chess_board.gm.alternate_turn(); // change turns()
            chess_board.gm.set_info(state, move, (Piece.PieceColor) (-selected_piece.get_piece_color()), selected_piece);
        }
        // INVALID MOVE
        else {
            selected_piece.GlobalPosition = original_position; // reset position, nothing changed
        }

        reset_selected_piece(); // always reset otherwise, invalid move
    }

    
}

public partial class Random : AI {
    Board chess_board;
    Piece.PieceColor color;
    RandomNumberGenerator rand = new RandomNumberGenerator();

    public Random(Board chess_board, Piece.PieceColor color) : base(chess_board, color){
        this.chess_board = chess_board;
        this.color = color;
    }

    public override void make_move() {
        set_random_piece(); // set the ref to selected_piece variable

        if (get_all_moves().Count != 0) {
            int rand_move_idx = rand.RandiRange(0, get_all_moves().Count - 1);
            place_selected_piece(get_all_moves()[rand_move_idx].get_tuple_reversed()); 
        }
        else{
            reset_selected_piece(); // directly from Player.cs
            Tuple<Piece, bool> can_continue = move_possible();
            if (can_continue.Item2) {
                set_selected_piece(can_continue.Item1);
                make_move();
            }
        }


    }

    // 
    public override List<Move> get_all_moves()
    {
        List<Move> return_moves = valid_moves.get_all_movement();
        if (return_moves == null || return_moves.Count == 0) {
            return new List<Move>();
        }
        return return_moves;
    }

    public void set_random_piece() {
        List<Piece> pr = chess_board.PieceRefs[(int) color]; // quick ref piecerefs

        int rand_piece_idx = rand.RandiRange(0, pr.Count - 1);

        Piece piece = pr[rand_piece_idx];

        set_selected_piece(piece);
    }

    private Tuple<Piece, bool> move_possible() {
        foreach (Piece p in chess_board.PieceRefs[(int) color]) {
            MoveManager mvm = new MoveManager(p, chess_board);
            Vector2 original_position = p.get_vector_position();
            mvm.get_cardinal_movement(original_position); // set all cardinal
            mvm.get_intermediate_movement(original_position);
            if (p.get_piece_type() == Piece.PieceType.Knight) {
                mvm.get_knight_movement(original_position);
            }
            if (p.get_piece_type() == Piece.PieceType.King) {
                mvm.get_castle(original_position, p);
            }
            if (p.get_piece_type() == Piece.PieceType.Pawn) {
                mvm.get_en_passant(original_position);
            }
            if (mvm.get_all_movement().Count > 0) {
                return new Tuple<Piece, bool>(p, true);
            }
        }
        return new Tuple<Piece, bool>(null, false);
    }
}

// https://www.chessprogramming.org/Alpha-Beta
public partial class AlphaBeta : AI {
    Board chess_board;
    Piece.PieceColor color;

    Piece[,] simulate_board;
    Dictionary<int, List<Piece>> simulate_pr;

    Evaluate eval;

    public AlphaBeta(Board chess_board, Piece.PieceColor color) : base(chess_board, color){
        this.chess_board = chess_board;
        this.color = color;
    }

public override void make_move() {

    Move bestMoveFound = null;
    double bestScore = double.NegativeInfinity;
    

    double alpha = -999999;
    double beta = 999999;
    int depth = 2;  
    

    double score = alphabeta(chess_board, alpha, beta, depth, true, ref bestMoveFound);
    

    if (bestMoveFound != null) {
        GD.Print("Best move found: " + bestMoveFound.ToString() + 
               " with score: " + score);
    } else {
        GD.Print("No valid move found!");
    }

     if (bestMoveFound != null) {
         set_selected_piece(bestMoveFound.get_piece());
         place_selected_piece(bestMoveFound.get_tuple_reversed());
     }
}
    public List<Move> get_all_moves(Piece.PieceColor pcol, Board chess_board)
    {
        List<Move> return_moves = new List<Move>();
        
        foreach (Piece p in chess_board.PieceRefs[(int) pcol]) {
            MoveManager mvm = new MoveManager(p, chess_board);
            Vector2 original_position = p.get_vector_position();
            mvm.get_cardinal_movement(original_position); // set all cardinal
            mvm.get_intermediate_movement(original_position);
            if (p.get_piece_type() == Piece.PieceType.Knight) {
                mvm.get_knight_movement(original_position);
            }
            if (p.get_piece_type() == Piece.PieceType.King) {
                mvm.get_castle(original_position, p);
            }
            if (p.get_piece_type() == Piece.PieceType.Pawn) {
                mvm.get_en_passant(original_position);
            }
            foreach (Move m in mvm.get_all_movement()) {
                return_moves.Add(m);
            }
        }

        return return_moves;
    }

    public override List<Move> get_all_moves(){ return new List<Move>();}

// Modified alphabeta with move tracking
public double alphabeta(Board board, double alpha, double beta, int depth, bool is_maximizing, ref Move bestMove) {
    if (depth == 0) {
        return evaluate(board.BoardTiles);
    }

    List<Move> moves = get_all_moves(is_maximizing ? color : (Piece.PieceColor)(-(int)color), board);
    Move currentBestMove = null;
    double bestValue = is_maximizing ? double.NegativeInfinity : double.PositiveInfinity;

    foreach (Move move in moves) {
        Board newBoard = simulate_move(board, move.get_piece(), move.get_tuple_reversed());
        double value = alphabeta(newBoard, alpha, beta, depth - 1, !is_maximizing, ref bestMove);
        
        if (is_maximizing) {
            if (value > bestValue) {
                bestValue = value;
                currentBestMove = move; 
                alpha = Math.Max(alpha, bestValue);
            }
        } else {
            if (value < bestValue) {
                bestValue = value;
                currentBestMove = move; 
                beta = Math.Min(beta, bestValue);
            }
        }
        
        if (beta <= alpha) break;
    }

    if (depth == 2 && currentBestMove != null) {
        bestMove = currentBestMove;
    }

    return bestValue;
}

    private Board simulate_move(Board b,Piece p, Tuple<int, int> move) {
        Board chess_board = new Board(b.gm, b.WriteForsythEdwards().Item2);

        Piece selected_piece = chess_board.BoardTiles[p.get_board_position().Item1, p.get_board_position().Item2]; 

        Vector2 original_position = selected_piece.GlobalPosition;


        // get the possible moves
        MoveManager mvm = new MoveManager(selected_piece, chess_board);
        valid_moves = mvm; // to get valid moves
        mvm.get_cardinal_movement(original_position); // set all cardinal
        mvm.get_intermediate_movement(original_position);
        if (p.get_piece_type() == Piece.PieceType.Knight) {
            mvm.get_knight_movement(original_position);
        }
        if (p.get_piece_type() == Piece.PieceType.King) {
            mvm.get_castle(original_position, p);
        }
        if (p.get_piece_type() == Piece.PieceType.Pawn) {
            mvm.get_en_passant(original_position);
        }  

        selected_piece.phist = chess_board.make_move(selected_piece, move); // assign phist MOVE IS MADE HERE
        selected_piece.ChangeState(Piece.State.Placed);                    

        // CASTLE LOGIC, 2 tile distance. No need to fix king position 

        if (selected_piece.get_piece_type() == Piece.PieceType.King &&
        Math.Abs(selected_piece.get_vector_position().X - original_position.X) == chess_board.CELL_SIZE * 2 
        && !chess_board.is_checked((Piece.PieceColor)selected_piece.get_piece_color(), chess_board.BoardTiles,
        new Tuple<int, int>((int)original_position.Y / chess_board.CELL_SIZE, (int)original_position.X / chess_board.CELL_SIZE)).Item1)  
        {
            if (Math.Abs(move.Item2 - 7) < Math.Abs(move.Item2) && chess_board.BoardTiles[move.Item1, chess_board.DIMENSION_X - 1] != null
            && chess_board.BoardTiles[move.Item1, chess_board.DIMENSION_X - 1].get_state() == Piece.State.Unmoved) {
                Piece rookr = chess_board.BoardTiles[move.Item1, chess_board.DIMENSION_X - 1];
                Tuple<int, int> castle_right = new Tuple<int, int>(move.Item1, move.Item2 - 1);
                rookr.phist = chess_board.make_move(rookr, castle_right);
                rookr.ChangeState(Piece.State.Placed);
            }
            else {
                Piece rookl = chess_board.BoardTiles[move.Item1, 0];
                Tuple<int, int> castle_left = new Tuple<int, int>(move.Item1, move.Item2 + 1);
                rookl.phist = chess_board.make_move(rookl, castle_left);
                rookl.ChangeState(Piece.State.Placed);
            }
        }

        // ENPASSANT LOGIC, no null check since en passant square auto updated, perspective of attacker
        if (chess_board.en_passant_square != null && selected_piece.get_piece_type() == Piece.PieceType.Pawn && 
        chess_board.en_passant_square.ToString().Equals(move.ToString())) {
            Piece passed_pawn = chess_board.BoardTiles[move.Item1 - selected_piece.get_piece_color(), move.Item2];
            passed_pawn.ChangeState(Piece.State.Captured);   
        }
        chess_board.en_passant_square = null; // always set en passant to null, opporunity passes
            // set en passant square from perspective of setter
        if (selected_piece.get_piece_type() == Piece.PieceType.Pawn 
            && Math.Abs(selected_piece.get_vector_position().Y - original_position.Y) == chess_board.CELL_SIZE * 2 ) 
        {
            chess_board.en_passant_square = new Tuple<int, int>(move.Item1 - selected_piece.get_piece_color(), move.Item2);
        }

        // PAWN PROMOTION LOGIC, no color check required since pawns naturally cant go backwards
        if (selected_piece.get_piece_type() == Piece.PieceType.Pawn ) {
            if (move.Item1 == 0 || move.Item1 == chess_board.DIMENSION_Y) {
                chess_board.gm.promote_pawn(selected_piece);
                
            }
        } // also remember to check for checks and checkmates after


        // CHECKING LOGIC

        // get the king of opposite color, you are the attacker
        // also get the color of the possible blockers
        Piece king = (selected_piece.get_piece_color() == (int) Piece.PieceColor.White) ? Board.Black_King : Board.White_King;
        Piece.PieceColor col = (selected_piece.get_piece_color() == (int) Piece.PieceColor.White) ? Piece.PieceColor.Black : Piece.PieceColor.White;
        // handle check and checkmate

        string state = "";

        // state set here. Perspective of hte attacker
        if (chess_board.is_checked(col, chess_board.BoardTiles).Item1) {
            state = "Check";
            if (chess_board.is_checkmated(col, chess_board.BoardTiles, king.get_threats(king.get_board_position(), chess_board.BoardTiles)[0])) {
                GameManager.GameState gs = (selected_piece.get_piece_color() == (int)Piece.PieceColor.White) ? GameManager.GameState.White_win : GameManager.GameState.Black_win;
                chess_board.gm.set_state(gs);
                state = "Checkmate";
            }
        }

        return chess_board;
    }

    private void push_move(Move m) {
        set_selected_piece(m.get_piece());
        place_selected_piece(m.get_tuple_reversed());
    }

    public double evaluate(Piece[,] array) {
        eval = new Evaluate(chess_board, array);
        return eval.eval();
    }


    
}
