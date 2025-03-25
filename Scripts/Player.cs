using Godot;
using System;
using System.Collections.Generic;

public partial class Player : Node2D
{   

    Board board;

    // piece states for selecting pieces
    bool piece_picked = false; // there is a piece being selected
    Piece selected_piece = null; // selected piece in play

    MoveManager valid_moves; // movemanager for valid moves
    Vector2 original_position = Vector2.Zero; // selected piece original position

    
    public Player(Board chess_board)
    {
        board = chess_board;
    }
    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        // start move
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
                        mvm.get_knight_movement(original_position);
                    }

                }
               
            }
            // check if move was valid after dropping it. Drag condition is in the else block below
            else if (!mouseClicked.Pressed && mouseClicked.ButtonIndex == MouseButton.Left && piece_picked == true) {
                //GD.Print(get_piece_under_mouse() + "undermouse");
                piece_picked = false; // set the piece selection to none
                Tuple<int, int> move = board.GetIndexUnderMouse(); // contains the "move" being made, in the form (row, col)
                bool success = false; // piece invalid until true
                // check conditions to make the move if it exists
                // MOVE must exist for move_validation to even run (see Board.cs)
                if (valid_moves.get_move_list_strings().Contains(move.ToString())) {
                    success = board.move_validation(selected_piece, move);
                    
                }

                // if the move was valid, make changes on the board! and reset
                if (success) {
                    selected_piece.ChangeState(Piece.State.Placed);
                    selected_piece.phist = board.make_move(selected_piece, move); // assign phist MOVE IS MADE HERE
                    
                    // Check if the same color King is under attack
                    if (board.is_checked((Piece.PieceColor) selected_piece.get_piece_color(), board.BoardTiles) == true) {
                        board.unmake_move(selected_piece.phist);
                        reset_selected_piece();
                        return;
                    }
                    // CHECK for CHECK AND CHECKMATE HERE !!!
                    else{
                        // check if you are checking the opposite color King
                        MoveManager mvm = new MoveManager(selected_piece, board);
                        //valid_moves = mvm; // to get valid moves
                        original_position = selected_piece.GlobalPosition;
                        mvm.get_cardinal_movement(original_position); // set all cardinal
                        mvm.get_intermediate_movement(original_position);
                        mvm.get_knight_movement(original_position);

                        // get the king of opposite color, you are the attacker
                        // also get the color of the possible blockers
                        Piece king = (selected_piece.get_piece_color() == (int) Piece.PieceColor.White) ? Board.Black_King : Board.White_King;
                        Piece.PieceColor col = (selected_piece.get_piece_color() == (int) Piece.PieceColor.White) ? Piece.PieceColor.Black : Piece.PieceColor.White;
                        // handle check and checkmate
                        if (mvm.get_move_list_strings().Contains(king.get_board_position().ToString())) {
                            board.temp_label.Text = "Check";
                            
                            if (board.is_checkmated(col, board.BoardTiles, selected_piece)) {
                                board.temp_label.Text = "Checkmate";
                            }
                        }
                        else{
                            board.temp_label.Text = "";
                        }

                        
                    }


                    // EVERYTHING HERE MEANS PLAYER SUCCESS
                    board.gm.alternate_turn(); // change turns()

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
                mouse_drag.X -= 16f;
                mouse_drag.Y -= 16f;

                selected_piece.GlobalPosition = mouse_drag;
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