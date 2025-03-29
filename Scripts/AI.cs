using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

// classes 
 public abstract partial class AI : Node2D
{   
    Board chess_board;
    Piece.PieceColor color;
    
    public enum BotType {
        Random = 0,
        AlphaBeta = 1,
        MonteCarlo = 2,
    }

    public AI(Board chess_board, Piece.PieceColor color){
        this.chess_board = chess_board;
        this.color = color;
    }

    public abstract void get_move();

}

public partial class Random : AI {
    Board chess_board;

    public Random(Board chess_board, Piece.PieceColor color) : base(chess_board, color){

    }

    public override void get_move() {
        Dictionary<int, List<Piece>> pr = chess_board.PieceRefs; // quick ref piecerefs

        RandomNumberGenerator rand = new RandomNumberGenerator();
        
    }
}

// https://www.chessprogramming.org/Alpha-Beta
public partial class AlphaBeta : AI {
    Board chess_board;

    public AlphaBeta(Board chess_board, Piece.PieceColor color) : base(chess_board, color){}

    public override void get_move() {

    }
}
