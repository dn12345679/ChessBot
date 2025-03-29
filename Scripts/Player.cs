using Godot;
using System;

public partial class Player : Node2D
{   
    Piece.PieceColor color;
    Board board;

    // piece states for selecting pieces
    bool piece_picked = false; // there is a piece being selected
    Piece selected_piece = null; // selected piece in play

    MoveManager valid_moves; // movemanager for valid moves
    Vector2 original_position = Vector2.Zero; // selected piece original position

    string state = ""; // just a text string to set for check/checkmate. Does not interact with game

    
    public Player(Board chess_board, Piece.PieceColor color)
    {
        board = chess_board;
        this.color = color;
    }

    /*
        All things related to Player input
        Handles dragging, state changes, and check/checkmate 
    */
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        // start move
        // if it is your color
        if (board.gm.get_current_turn().ToString() == color.ToString()) {
            if (@event is InputEventMouseButton mouseClicked) {
                if (mouseClicked.Pressed && mouseClicked.ButtonIndex == MouseButton.Left) {
                    
                    Piece p = get_piece_under_mouse();
                    // check valid piece acquired
                    if (p != null) 
                    {   
                        // make sure to reset these attributes if unselected
                        if (p.get_piece_color() == (int) board.gm.get_current_turn()) 
                        {
                            selected_piece = p; 
                            piece_picked = true; // th ere is a piece being selected
                            original_position = p.GlobalPosition;


                            // get the possible moves
                            MoveManager mvm = new MoveManager(p, board);
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
                // check if move was valid after dropping it. Drag condition is in the else block below
                else if (!mouseClicked.Pressed && mouseClicked.ButtonIndex == MouseButton.Left && piece_picked == true) {
                    //GD.Print(get_piece_under_mouse() + "undermouse");
                    piece_picked = false; // set the piece selection to none
                    Tuple<int, int> move = board.GetIndexUnderMouse(); // contains the "move" being made, in the form (row, col) or NULL
                    bool success = false; // piece invalid until true
                    
                    // check conditions to make the move if it exists
                    // MOVE must exist for move_validation to even run (see Board.cs)
                    success = valid_moves != null && move != null && valid_moves.get_move_list_strings().Contains(move.ToString());          
                    

                    // if the move was valid, make changes on the board! and reset
                    if (success) {
                        selected_piece.phist = board.make_move(selected_piece, move); // assign phist MOVE IS MADE HERE
                        selected_piece.ChangeState(Piece.State.Placed);                    

                        //
                        
                        // CASTLE LOGIC, 2 tile distance. No need to fix king position 

                        if (selected_piece.get_piece_type() == Piece.PieceType.King &&
                        Math.Abs(selected_piece.get_vector_position().X - original_position.X) == board.CELL_SIZE * 2 
                        && !board.is_checked((Piece.PieceColor)selected_piece.get_piece_color(), board.BoardTiles,
                        new Tuple<int, int>((int)original_position.Y / 32, (int)original_position.X / 32)))  
                        {
                            if (Math.Abs(move.Item2 - 7) < Math.Abs(move.Item2) && board.BoardTiles[move.Item1, board.DIMENSION_X - 1] != null
                            && board.BoardTiles[move.Item1, board.DIMENSION_X - 1].get_state() == Piece.State.Unmoved) {
                                Piece rookr = board.BoardTiles[move.Item1, board.DIMENSION_X - 1];
                                Tuple<int, int> castle_right = new Tuple<int, int>(move.Item1, move.Item2 - 1);
                                rookr.phist = board.make_move(rookr, castle_right);
                                rookr.ChangeState(Piece.State.Placed);
                            }
                            else {
                                Piece rookl = board.BoardTiles[move.Item1, 0];
                                Tuple<int, int> castle_left = new Tuple<int, int>(move.Item1, move.Item2 + 1);
                                rookl.phist = board.make_move(rookl, castle_left);
                                rookl.ChangeState(Piece.State.Placed);
                            }
                        }

                        // ENPASSANT LOGIC, no null check since en passant square auto updated, perspective of attacker
                        if (board.en_passant_square != null && selected_piece.get_piece_type() == Piece.PieceType.Pawn && 
                        board.en_passant_square.ToString().Equals(move.ToString())) {
                            Piece passed_pawn = board.BoardTiles[move.Item1 - selected_piece.get_piece_color(), move.Item2];
                            passed_pawn.ChangeState(Piece.State.Captured);   
                        }
                        board.en_passant_square = null; // always set en passant to null, opporunity passes
                            // set en passant square from perspective of setter
                        if (selected_piece.get_piece_type() == Piece.PieceType.Pawn 
                            && Math.Abs(selected_piece.get_vector_position().Y - original_position.Y) == board.CELL_SIZE * 2 ) 
                        {
                            board.en_passant_square = new Tuple<int, int>(move.Item1 - selected_piece.get_piece_color(), move.Item2);
                        }

                        // PAWN PROMOTION LOGIC, no color check required since pawns naturally cant go backwards
                        if (selected_piece.get_piece_type() == Piece.PieceType.Pawn ) {
                            if (move.Item1 == 0 || move.Item1 == board.DIMENSION_Y) {
                                board.gm.promote_pawn(selected_piece);
                                
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
                        if (board.is_checked(col, board.BoardTiles)) {
                            state = "Check";
                            if (board.is_checkmated(col, board.BoardTiles, king.get_threats(king.get_board_position(), board.BoardTiles)[0])) {
                                GameManager.GameState gs = (selected_piece.get_piece_color() == (int)Piece.PieceColor.White) ? GameManager.GameState.White_win : GameManager.GameState.Black_win;
                                board.gm.set_state(gs);
                                state = "Checkmate";
                            }
                        }
                        

                        // EVERYTHING HERE MEANS PLAYER SUCCESS
                        board.gm.alternate_turn(); // change turns()
                        board.gm.set_info(state, move, (Piece.PieceColor) (-selected_piece.get_piece_color()), selected_piece);
                    }
                    // INVALID MOVE
                    else {
                        selected_piece.GlobalPosition = original_position; // reset position, nothing changed
                    }

                    reset_selected_piece(); // always reset otherwise, invalid move
                }
            }
            // piece being dragged?
            else if (@event is InputEventMouseMotion mouseMoved) {
                if (selected_piece != null) {
                    // get the mouse current position with some offset
                    Vector2 mouse_drag = GetGlobalMousePosition();
                    mouse_drag.X -= 16f; // fix offset
                    mouse_drag.Y -= 16f;

                    selected_piece.GlobalPosition = mouse_drag;
                }
            }
        }

    }

    // resets all references to the previously selected piece
    // no returns
    public void reset_selected_piece() {
        selected_piece = null; // piece being selected
        piece_picked = false; // boolean flag for piece selection
        valid_moves = null; // MoveManager object, responsible for containing moves
    }

    /*
    Returns the "Piece" object under the current mouse position if it exists,
    Returns null otherwise.
    No parameters
    */
    public Piece get_piece_under_mouse() {
        Tuple<int, int> idx = board.GetIndexUnderMouse(); // board class implements this
        if (idx != null) {
            return board.BoardTiles[idx.Item1, idx.Item2];
        }
        return null;

        
    }




}