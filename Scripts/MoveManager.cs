using Godot;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;

public partial class MoveManager : Node2D {
    
    private Vector2 piece_current_position = Vector2.Zero;
    private Piece current_piece; 
    private Board board;


    private List<Move> cardinal_moves;
    private List<Move> intermediate_moves;

    

    public MoveManager(Piece piece, Board board) {
        this.board = board;
        this.current_piece = piece;
        piece_current_position = piece.get_vector_position();
    }

    // returns a list of all Move objects related to this piece as Strings.
    // Dependent on get_all_movement() being valid
    public List<String> get_move_list_strings() {
        List<String> moves = new List<String>();

        foreach (Move move in get_all_movement()) {
            GD.Print(move);
            moves.Add(move.ToString());
        }
        return moves;
    }

    // returns a list of all Move objects related to this piece.
    // Dependent on get_cardinal_movement() being run at least once
    public List<Move> get_all_movement() {
        List<Move> moves = new List<Move>();
        moves.AddRange(cardinal_moves); // add all cardinal moves if they exist

        return moves;
    }

    public List<Move> get_cardinal_movement(Vector2 current_position) {
        List<Move> moves= new List<Move>();

        switch (current_piece.get_piece_type()) {
            // pawn and first tmove of pawn
            case Piece.PieceType.Pawn:
                Move move = new Move(current_piece, board);
                moves.Add(move.move_if_valid(current_position + new Vector2(0, current_piece.get_piece_color() * board.CELL_SIZE)));
                if (current_piece.get_state() == Piece.State.Unmoved) {
                    move = new Move(current_piece, board);
                    moves.Add(move.move_if_valid(current_position + new Vector2(0, current_piece.get_piece_color() * board.CELL_SIZE * 2)));                    
                }
                break;
        }
        cardinal_moves = moves;
        return moves;
    }

    // toDo:
    public List<Move> get_intermediate_movement(Vector2 current_position) {
        List<Move> moves= new List<Move>();
        switch (current_piece.get_piece_type()) {
            // pawn and first tmove of pawn
            case Piece.PieceType.Pawn:

                break;
        }
        intermediate_moves = moves;
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
    public Move move_if_valid(Vector2 physical_position = new Vector2()) { 
        
        // checks
        if (physical_position != new Vector2() ) {
            // move_position is a tuple containing the indexing positions of a move check
            Tuple<int, int> move_position = new Tuple<int, int>((int)physical_position.X / 32, (int)physical_position.Y / 32);
            // null check and same color check
            if ((board.BoardTiles[move_position.Item2, move_position.Item1] == null) 
                || (board.BoardTiles[move_position.Item2, move_position.Item1].get_piece_color() != piece.get_piece_color())) {
                this.move_position = move_position; // update this move object to containt the valid move
                return this;
            }

        }
        return null;
    }

    public override string ToString()
    {
        return "(" + move_position.Item2 + ", " + move_position.Item1 + ")";

    }
}