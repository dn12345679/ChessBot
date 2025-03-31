using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Xml.Schema;


public partial class MoveManager : Node2D {
    
    private Vector2 piece_current_position = Vector2.Zero;
    private Piece current_piece; 
    private Board board;


    private List<Move> cardinal_moves = new List<Move>();
    private List<Move> intermediate_moves = new List<Move>();
    private List<Move> knight_moves = new List<Move>();
    private List<Move> castle_moves = new List<Move>();
    private List<Move> enpassant_moves = new List<Move>();

    /* 
        Optimization:
            - Check if piece is pinned:
                - If king is not checked, moves are only valid if its in the direction of the attacker
                - if king is checked, DON"T get any moves for this piece. 
            - Check if piece is not pinned:
                - if king is not checked, CONTINUE, free to move
                - if king is checked, moves are only valid if it intersects with the vector between the king and attacker
    */
    // 
    private bool validate_optimizer(Move move) {
        if (board.move_validation(current_piece, new Tuple<int, int>(move.get_tuple().Item2, move.get_tuple().Item1), 
        get_check_info(new Tuple<int, int>(move.get_tuple().Item2, move.get_tuple().Item1)))) {
            return true;
        }
        return false;
    }
    // Related to above method. Early return optimization
    private bool pinned_and_checked() {
        if (current_piece.is_pinned(current_piece).Item1)
        {
            if (board.is_checked((Piece.PieceColor)current_piece.get_piece_color(), board.BoardTiles).Item1) {
                return true;
            }                    
        }
        return false;
    }

    private Tuple<bool, List<Piece>> get_check_info(Tuple<int, int> move) {
        GD.Print(board);
        GD.Print(move);
        GD.Print(board.is_checked((Piece.PieceColor) current_piece.get_piece_color(), board.BoardTiles, move));
        GD.Print(" is checked " + String.Join(", ", board.is_checked((Piece.PieceColor) current_piece.get_piece_color(), board.BoardTiles, move).Item2));
        return board.is_checked((Piece.PieceColor) current_piece.get_piece_color(), board.BoardTiles, move);
    }

    private bool is_attacker_square(Move move) {
        List<Piece> attackers = new List<Piece>();
        Tuple<int, int> idx_corrected = new Tuple<int, int>(move.get_tuple().Item2, move.get_tuple().Item1);
        switch ((Piece.PieceColor) current_piece.get_piece_color()) {
            case Piece.PieceColor.Black:
                attackers = Board.Black_King.get_threats(Board.Black_King.get_board_position(), board.BoardTiles);
                break;
            case Piece.PieceColor.White:
                attackers = Board.White_King.get_threats(Board.White_King.get_board_position(), board.BoardTiles);
                break;
        }
        
        return attackers.Count == 0 || attackers.Any(item => item.get_board_position().ToString().Equals(idx_corrected.ToString()));
    }

    public MoveManager(Piece piece, Board board) {
        this.board = board;
        this.current_piece = piece;
        piece_current_position = piece.get_vector_position();
    }

    // returns a list of all Move objects related to this piece as Strings.
    // Dependent on get_all_movement() being valid
    public List<string> get_move_list_strings() {
        List<string> moves = new List<string>();

        foreach (Move move in get_all_movement()) {
            //GD.Print(move);
            moves.Add(move.ToString());
        }
        return moves;
    }

    // returns a list of all Move objects related to this piece.
    // Dependent on get_cardinal_movement() being run at least once

    // TODO: Not implemented as of March 20, 2025 4:40 PM
    //  Castling: idea, use the piece firstMove boolean flag, 
    //  En passant: Not done

    public List<Move> get_all_movement() {
        // possible idea: run get_cardinal_movement and get_intermediate_movement by default
        // on current_piece
        List<Move> moves = new List<Move>();
        if (cardinal_moves != null && cardinal_moves.Count > 0) {moves.AddRange(cardinal_moves);} // add all cardinal moves if they exist
        if (intermediate_moves != null && intermediate_moves.Count > 0) {moves.AddRange(intermediate_moves);} // add all intermediate moves if they exist
        if (knight_moves != null && knight_moves.Count > 0) {moves.AddRange(knight_moves);}
        if (castle_moves != null && castle_moves.Count > 0) {moves.AddRange(castle_moves);}
        if (enpassant_moves != null && enpassant_moves.Count > 0) {moves.AddRange(enpassant_moves);}

        return moves;
    }


    // gets all the possible cardinal movements of a piece.
    // Does not account for things like "king exposure"
    // See Piece.cs to see how "king exposure" is handled
    // Returns a List of ALL valid moves legal or not
    // Mod guide:
        // Either add a new case, or change the Move class. Dependnet on Piece.cs 
    // Handles Pawn First move 
    public List<Move> get_cardinal_movement(Vector2 current_position) {
        List<Move> moves = new List<Move>();

        switch (current_piece.get_piece_type()) {
            // pawn and first tmove of pawn
            case Piece.PieceType.Pawn:
                Move move = new Move(current_piece, board);
                Tuple<Move, int> sq_move = move.move_if_valid(current_position + new Vector2(0, current_piece.get_piece_color() * board.CELL_SIZE));
                if (sq_move == null) {
                    break; // don't bother checking double square. If blocked then break
                }
                else{
                    GD.Print(move);
                    if (validate_optimizer(move)) {
                        moves.Add(sq_move.Item1);
                    }
                    // only second move if unmoved
                    if (current_piece.get_state() == Piece.State.Unmoved) {
                        Move move_double = new Move(current_piece, board);
                        Tuple<Move, int> sq_move_double = move_double.move_if_valid(current_position + new Vector2(0, current_piece.get_piece_color() * board.CELL_SIZE * 2));
                        if (sq_move_double != null && validate_optimizer(move_double)) {
                            // valid double, add it
                            moves.Add(sq_move_double.Item1); 
                        }            
                    }
                }

                break;
            // rook, king, queen
            case Piece.PieceType.Rook:
            case Piece.PieceType.King:
            case Piece.PieceType.Queen:
                // dir loop guide:
                    // 0 = north, 1 = east, 2 = south, 3 = west.
                    // this is irrelevant outside of this code chunk
                
                Vector2[] directions = new Vector2[4] {new Vector2(0, -1), 
                                                       new Vector2(1, 0), 
                                                       new Vector2(0, 1), 
                                                       new Vector2(-1, 0) };

                if (pinned_and_checked()) { return moves;}

                for (int dir = 0; dir < 4; dir++) {
                    // loops starting from 1
                    for (int tile = 1; tile < 8; tile++) {
                        Move move_default = new Move(current_piece, board);
                        //GD.Print(current_position + directions[dir] * board.CELL_SIZE * tile);
                        Tuple<Move, int> sq_move_default = move_default.move_if_valid(current_position + directions[dir] * board.CELL_SIZE * tile);
                        // early breaks prevent extra iterations
                        if(sq_move_default == null) {
                            break;    
                        }
                        
                        // optimization 3/25/25, don't add moves to king moves if it results in a check
                        if (current_piece.get_piece_type() == Piece.PieceType.King) {
                            Tuple<int, int> tup2move = new Tuple<int, int>(sq_move_default.Item1.get_tuple().Item2, sq_move_default.Item1.get_tuple().Item1);
                            if (board.is_checked((Piece.PieceColor) current_piece.get_piece_color(), board.BoardTiles, tup2move).Item1) {
                                break;
                            }
                        }
                        // only need to check the very first tile.
                        if (tile == 1 && !validate_optimizer(sq_move_default.Item1)) { 
                            break;
                        }
                        moves.Add(sq_move_default.Item1); // valid move
                        
                        // Since King can only move 1 square, break early
                        if (current_piece.get_piece_type() == Piece.PieceType.King) {
                            break;
                        }
                        // check if the square contains a non-null tile (guaranteed to be of opposite color)
                        if (sq_move_default.Item2 != (int) Piece.PieceColor.Default) 
                        {
                            break; // don't continue searching if you are blocked
                        }
                    }
                }
                break;

        }
        cardinal_moves = moves;
        return moves;
    }

    // gets all the possible intermediate movements of a piece.
    // Does not account for things like "king exposure"
    // See Piece.cs to see how "king exposure" is handled
    // REturns a list of ALL valid moves Legal or not
    // Mod guide:
        // Either add a new case, or change the Move class. Dependent on Piece.cs
    public List<Move> get_intermediate_movement(Vector2 current_position) {
        List<Move> moves= new List<Move>();

        switch (current_piece.get_piece_type()) {
            // pawn and first tmove of pawn
            case Piece.PieceType.Pawn:
                // left and right diagonals
                Move move = new Move(current_piece, board);
                Tuple<Move, int> sq_move = move.move_if_valid(current_position + new Vector2(-1, current_piece.get_piece_color()) * board.CELL_SIZE);
                
                Move move_2 = new Move(current_piece, board);
                Tuple<Move, int> sq_move_2 = move_2.move_if_valid(current_position + new Vector2(1, current_piece.get_piece_color()) * board.CELL_SIZE);
                
                // only add move if the tile contains the opposite color
                if (sq_move != null && sq_move.Item2 != (int) Piece.PieceColor.Default && validate_optimizer(sq_move.Item1)) {moves.Add(sq_move.Item1);}
                if (sq_move_2 != null && sq_move_2.Item2 != (int) Piece.PieceColor.Default && validate_optimizer(sq_move_2.Item1)){moves.Add(sq_move_2.Item1);}
                
                break;
            // rook, king, queen
            case Piece.PieceType.Bishop:
            case Piece.PieceType.King:
            case Piece.PieceType.Queen:
                // dir loop guide:
                    // 0 = northwest, 1 = southeast, 2 = southwest, 3 = northeast.
                    // this is irrelevant outside of this code chunk
                
                Vector2[] directions = new Vector2[4] {new Vector2(-1, -1), 
                                                       new Vector2(1, 1), 
                                                       new Vector2(-1, 1), 
                                                       new Vector2(1, -1) };
                for (int dir = 0; dir < 4; dir++) {
                    // loops starting from 1
                    for (int tile = 1; tile < 8; tile++) {
                        Move move_default = new Move(current_piece, board);
                        //GD.Print(current_position + directions[dir] * board.CELL_SIZE * tile);
                        Tuple<Move, int> sq_move_default = move_default.move_if_valid(current_position + directions[dir] * board.CELL_SIZE * tile);
                        // early breaks prevent extra iterations
                        if(sq_move_default == null) {
                            break;
                        }

                        // optimization 3/25/25, don't add moves to king if it results in a check
                        if (current_piece.get_piece_type() == Piece.PieceType.King) {
                            Tuple<int, int> tup2move = new Tuple<int, int>(sq_move_default.Item1.get_tuple().Item2, sq_move_default.Item1.get_tuple().Item1);
                            if (board.is_checked((Piece.PieceColor) current_piece.get_piece_color(), board.BoardTiles, tup2move).Item1) {
                                break;
                            }
                        }
                        // optimize 3/28/25
                        if (tile == 1 && !validate_optimizer(sq_move_default.Item1)) { 
                            break;
                        }
                        moves.Add(sq_move_default.Item1); // valid move
                        
                        // Since King can only move 1 square, break early
                        if (current_piece.get_piece_type() == Piece.PieceType.King) {
                            break;
                        }
                        // check if the square contains a non-null tile (guaranteed to be of opposite color)
                        if (sq_move_default.Item2 != (int) Piece.PieceColor.Default) 
                        {
                            break; // don't continue searching if you are blocked
                        }
                    }
                }
                break;

        }
        intermediate_moves = moves; // sets the intermediate moves
        return moves;
    }
    
    // gets all the possible knight movements of a piece
    // Does not account for things like "king exposure"
    // See Piece.cs to see how "king exposure" is handled
    // Returns a list of all valid moves legal or not
    // Mod guide:
        // This works very differently than the other 2, so figure it out urself ig 
    public List<Move> get_knight_movement(Vector2 current_position) {
        List<Move> moves = new List<Move>();

        if (current_piece.is_pinned(current_piece).Item1) {return moves;}

        if (current_piece.get_piece_type() == Piece.PieceType.Knight) {
            Vector2[] km = new Vector2[8] {
                new Vector2(2, 1),  new Vector2(2, -1),  // Right-Up, Right-Down
                new Vector2(-2, 1), new Vector2(-2, -1), // Left-Up, Left-Down
                new Vector2(1, 2),  new Vector2(1, -2),  // Up-Right, Down-Right
                new Vector2(-1, 2), new Vector2(-1, -2)  // Up-Left, Down-Left
            };
            foreach (Vector2 move_vec in km) {
                Move move = new Move(current_piece, board);
                //GD.Print(current_position + directions[dir] * board.CELL_SIZE * tile);
                Tuple<Move, int> sq_move = move.move_if_valid(current_position + move_vec * board.CELL_SIZE);
                // early breaks prevent extra iterations
                if(sq_move == null) {
                    continue;
                }

                if (validate_optimizer(sq_move.Item1) && is_attacker_square(sq_move.Item1)) { 
                    moves.Add(sq_move.Item1); // valid move
                }
               
            }
        }
        knight_moves = moves; // important: sets the knight moves so its accessible
        return moves;
    }

    /* 
    REturns the castling availability of the current board (either as set by FEN or by any change in movement) 
        Requirements for castle:
            - Neither the king nor rook at the respective position have moved
            - Space between both is empty
            - King position and castling position for king are not under attack
    */
    public List<Move> get_castle(Vector2 current_position, Piece p) {
        // king initiated castle from unmoved king. no index check required
        List<Move> moves = new List<Move>();
        if (p.get_piece_type() == Piece.PieceType.King && p.get_state() == Piece.State.Unmoved) {
            Tuple<int, int> kp = p.get_board_position();
            
            Piece rookr = board.BoardTiles[kp.Item1, kp.Item2 + 3];
            Piece rookl = board.BoardTiles[kp.Item1, kp.Item2 - 4];
            

            /* no piece type check required, since an unmoved piece in that position is by default
                a rook. Don't allow castling through pieces by default
            */
            if (rookr != null && rookr.get_state() == Piece.State.Unmoved) 
            {
                
                Move move = new Move(p, board);
                if ((board.BoardTiles[kp.Item1, kp.Item2 + 2] == null || board.BoardTiles[kp.Item1, kp.Item2 + 2].get_state() == Piece.State.Captured)
                && (board.BoardTiles[kp.Item1, kp.Item2 + 1] == null || board.BoardTiles[kp.Item1, kp.Item2 + 1].get_state() == Piece.State.Captured)) {
                    // right castle
                    Tuple<Move, int> sq_move = move.move_if_valid(current_position + new Vector2(board.CELL_SIZE * 2, 0));
                    if (sq_move != null && !board.is_checked((Piece.PieceColor) p.get_piece_color(), board.BoardTiles).Item1
                    && p.get_threats(p.get_board_position(), board.BoardTiles).Count == 0) {
                        moves.Add(sq_move.Item1); 
                        // add to movelist if king not in check, all tiles between king and rook are empty, and neither position is in check
                    }
                }
            }

            if (rookl != null && rookl.get_state() == Piece.State.Unmoved) 
            {
                Move move = new Move(p, board);
                if ((board.BoardTiles[kp.Item1, kp.Item2 - 3] == null || board.BoardTiles[kp.Item1, kp.Item2 - 3].get_state() == Piece.State.Captured)
                && (board.BoardTiles[kp.Item1, kp.Item2 - 2] == null || board.BoardTiles[kp.Item1, kp.Item2 - 2].get_state() == Piece.State.Captured)
                && (board.BoardTiles[kp.Item1, kp.Item2 - 1] == null || board.BoardTiles[kp.Item1, kp.Item2 - 1].get_state() == Piece.State.Captured)) {
                    // right castle
                    Tuple<Move, int> sq_move = move.move_if_valid(current_position - new Vector2(board.CELL_SIZE * 2, 0));
                    if (sq_move != null && !board.is_checked((Piece.PieceColor) p.get_piece_color(), board.BoardTiles).Item1
                    && p.get_threats(p.get_board_position(), board.BoardTiles).Count == 0) {
                        moves.Add(sq_move.Item1); 
                        // add to movelist if king not in check, all tiles between king and rook are empty, and neither position is in check
                    }
                }
            }
        }
        castle_moves = moves;
        return moves;
    }

    
    public List<Move> get_en_passant(Vector2 current_position) {
        
        List<Move> moves = new List<Move>();
        if (current_piece.get_piece_type() == Piece.PieceType.Pawn) {
            Move move = new Move(current_piece, board);
            Tuple<Move, int> sq_move = move.move_if_valid(current_position + new Vector2(-1, current_piece.get_piece_color()) * board.CELL_SIZE);
            
            Move move_2 = new Move(current_piece, board);
            Tuple<Move, int> sq_move_2 = move_2.move_if_valid(current_position + new Vector2(1, current_piece.get_piece_color()) * board.CELL_SIZE);
            
            // only add move if the tile is the current enpassant square
            if (sq_move != null && board.en_passant_square != null && sq_move.Item1.ToString().Equals(board.en_passant_square.ToString())) {moves.Add(sq_move.Item1);}
            if (sq_move_2 != null && board.en_passant_square != null && sq_move_2.Item1.ToString().Equals(board.en_passant_square.ToString())){moves.Add(sq_move_2.Item1);}
        }
        enpassant_moves = moves; 
        return moves;
    }

}



// A sub class only intended to validate and check if a move calculated is legal or not.
// Does not handle any logic in regards to the moves position
public partial class Move : Node2D {
    Board board;
    Piece piece;

    Tuple<int, int> move_position = new Tuple<int, int>(0,0);

    public Move(Piece piece, Board board) {
        this.board = board;
        this.piece = piece;
    }

    // Given either a physical board position Vector2 
    // check conditions for an individual piece (don't ocnsider the board):
    // - Move is in bounds (done)
    // - Move lands on an empty square or opposite color (done)
    // How do the indices work?
    //  --> Item2 first since its the "y" axis, but in a 2d array its the row array
    //  --> Item1 second since its the "x" axis, but in a 2d array its an index in the row array

    // Returns the Move object to be made, and the int piece color of the 
    public Tuple<Move, int> move_if_valid(Vector2 physical_position) { 

        // move_position is a tuple containing the indexing positions of a move check
        Tuple<int, int> move_position = new Tuple<int, int>((int)physical_position.X / 32, (int)physical_position.Y / 32);
        
        
        // index check, then null check and same color check 
        if (tuple_in_bounds(move_position)) {
            
            Piece piece_to = board.BoardTiles[move_position.Item2, move_position.Item1];
            // case 1: no piece there, keep going, OR piece that was ALREADY CAPTURED is there
            if (piece_to == null || piece_to.get_state() == Piece.State.Captured) {
                this.move_position = move_position; // update this move object to containt the valid move
                return new Tuple<Move, int>(this, (int) Piece.PieceColor.Default);
            }
            // case 2: opposite color piece
            // (break iteration here, handled by "get_xxx_movement()")
            else if(piece_to.get_piece_color() != piece.get_piece_color()) {
                if (piece.get_piece_type() == Piece.PieceType.Pawn && piece_to.get_board_position().Item2 == piece.get_board_position().Item2) {
                    return null; // stop pawns from going straight on other pieces
                }

                this.move_position = move_position; // update this move object to containt the valid move
                return new Tuple<Move, int>(this, piece_to.get_piece_color());                    
            }
            
        }
        
        return null;
    }

    // Given an input tuple "index_position" in the form
    // - Item1 = physical X position (file)
    // - Item2 = physical Y position (rank)
    // Return whether it is in bounds

    // static since I need it outside of instantiation
    public static bool tuple_in_bounds(Tuple<int, int> index_position) {
        return index_position.Item1 < 8 && index_position.Item1 > -1 && 
                index_position.Item2 < 8 && index_position.Item2 > -1;
    }

    // returns the move tuple of this move
    public Tuple<int, int> get_tuple() {
        return move_position;
    }
    public override string ToString()
    {
        return "(" + move_position.Item2 + ", " + move_position.Item1 + ")";

    }
}