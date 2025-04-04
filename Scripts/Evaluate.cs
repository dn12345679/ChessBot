using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;

public partial class Evaluate {
    Board board;

    public enum pieceVals {
        Pawn = 1,
        Knight = 3,
        Bishop = 3, 
        Rook = 5,
        Queen = 9,
    }

    public Dictionary<Piece.PieceType, int[]> PST = new Dictionary<Piece.PieceType, int[]>() {
        {Piece.PieceType.Pawn, new int[]{ 0,  0,  0,  0,  0,  0,  0,  0,
50, 50, 50, 50, 50, 50, 50, 50,
10, 10, 20, 30, 30, 20, 10, 10,
 5,  5, 10, 25, 25, 10,  5,  5,
 0,  0,  0, 20, 20,  0,  0,  0,
 5, -5,-10,  0,  0,-10, -5,  5,
 5, 10, 10,-20,-20, 10, 10,  5,
 0,  0,  0,  0,  0,  0,  0,  0}},
        {Piece.PieceType.Knight, new int[]{ -50,-40,-30,-30,-30,-30,-40,-50,
-40,-20,  0,  0,  0,  0,-20,-40,
-30,  0, 10, 15, 15, 10,  0,-30,
-30,  5, 15, 20, 20, 15,  5,-30,
-30,  0, 15, 20, 20, 15,  0,-30,
-30,  5, 10, 15, 15, 10,  5,-30,
-40,-20,  0,  5,  5,  0,-20,-40,
-50,-40,-30,-30,-30,-30,-40,-50}},
        {Piece.PieceType.Bishop, new int[] {-20,-10,-10,-10,-10,-10,-10,-20,
-10,  0,  0,  0,  0,  0,  0,-10,
-10,  0,  5, 10, 10,  5,  0,-10,
-10,  5,  5, 10, 10,  5,  5,-10,
-10,  0, 10, 10, 10, 10,  0,-10,
-10, 10, 10, 10, 10, 10, 10,-10,
-10,  5,  0,  0,  0,  0,  5,-10,
-20,-10,-10,-10,-10,-10,-10,-20,}},
        {Piece.PieceType.Rook, new int[] {  0,  0,  0,  0,  0,  0,  0,  0,
  5, 10, 10, 10, 10, 10, 10,  5,
 -5,  0,  0,  0,  0,  0,  0, -5,
 -5,  0,  0,  0,  0,  0,  0, -5,
 -5,  0,  0,  0,  0,  0,  0, -5,
 -5,  0,  0,  0,  0,  0,  0, -5,
 -5,  0,  0,  0,  0,  0,  0, -5,
  0,  0,  0,  5,  5,  0,  0,  0}}, 
        {Piece.PieceType.Queen, new int[] {-20,-10,-10, -5, -5,-10,-10,-20,
-10,  0,  0,  0,  0,  0,  0,-10,
-10,  0,  5,  5,  5,  5,  0,-10,
 -5,  0,  5,  5,  5,  5,  0, -5,
  0,  0,  5,  5,  5,  5,  0, -5,
-10,  5,  5,  5,  5,  5,  0,-10,
-10,  0,  5,  0,  0,  0,  0,-10,
-20,-10,-10, -5, -5,-10,-10,-20}}, 
        {Piece.PieceType.King, new int[] {-30,-40,-40,-50,-50,-40,-40,-30,
-30,-40,-40,-50,-50,-40,-40,-30,
-30,-40,-40,-50,-50,-40,-40,-30,
-30,-40,-40,-50,-50,-40,-40,-30,
-20,-30,-30,-40,-40,-30,-30,-20,
-10,-20,-20,-20,-20,-20,-20,-10,
 20, 20,  0,  0,  0,  0, 20, 20,
 20, 30, 10,  0,  0, 10, 30, 20}}
 
    
    };


    public Dictionary<Piece.PieceType, int> Mobility = new Dictionary<Piece.PieceType, int>() {
        {Piece.PieceType.Pawn,7},
        {Piece.PieceType.Knight,12},
        {Piece.PieceType.Bishop,12},
        {Piece.PieceType.Rook, 6},
        {Piece.PieceType.Queen, 3},
    };

    public Dictionary<Piece.PieceType, double> KingShield = new Dictionary<Piece.PieceType,double>() {
        {Piece.PieceType.Pawn,12},
        {Piece.PieceType.Knight, 0.8 * 12},
        {Piece.PieceType.Bishop, 0.8 * 12},
        {Piece.PieceType.Rook, 0.6 * 12},
        {Piece.PieceType.Queen, 0.3 * 12},        
    };

    public enum gameStage {
        early = 0,
        mid = 10,

    }

    public Evaluate(Board board) {
        this.board = board;
    }   

    public double eval() {
        double material_score = get_material_score(); // total material difference
        double PST_score = get_PST_score(); // piece square table 
        double mobility_score = get_mobility_score(); // move count total (weighted)

        double pawn_score = get_pawn_score(); // hanging pawns, promotability, blocked pawns, bishop blockers...?
        double bishop_bonus = get_bishop_availability(); // having both bishops is beneficial. Bishops gain more score with less pieces on their squares
        double rook_bonus = get_rook_availability(); // 

        double king_safety = get_king_safety(); // king gets higher score with more evasive moves (weighed heavier than mobility_score), 
        double offensive_rating = 0.0; // closer relative distance to opponent king


        return material_score + PST_score + 
                mobility_score + pawn_score + 
                bishop_bonus + king_safety + 
                offensive_rating + rook_bonus;
    }
    // Returns the material score of the board
    private double get_material_score() {

        double total = 0.0;

        Dictionary<int, List<Piece>> pr = board.PieceRefs;
        
        List<Piece> prWhite = pr[(int) Piece.PieceColor.White];
        List<Piece> prBlack = pr[(int) Piece.PieceColor.Black];

        
        int multiplying_factor = (int) Piece.PieceColor.White;
        foreach (List<Piece> plist in new List<List<Piece>>(){prWhite, prBlack}) {
            double temp_total = 0.0;
            foreach (Piece p in plist) {
                switch (p.get_piece_type()) {
                    case Piece.PieceType.Pawn:
                        temp_total += (int) pieceVals.Pawn;
                        break;
                    case Piece.PieceType.Knight:
                        temp_total += (int) pieceVals.Knight;
                        break;    
                    case Piece.PieceType.Bishop:
                        temp_total += (int) pieceVals.Bishop;
                        break;
                    case Piece.PieceType.Rook:
                        temp_total += (int) pieceVals.Rook;
                        break;
                    case Piece.PieceType.Queen:
                        temp_total += (int) pieceVals.Queen;
                        break;
                }
            }
            // after finished with iterating over A list
            temp_total *= multiplying_factor;
            multiplying_factor *= -1;
            total += temp_total; // add

        }

        return total;
    }


    // Returns the piece square table 
    private double get_PST_score() {
        double total = 0.0;
        Dictionary<int, List<Piece>> pr = board.PieceRefs;
        
        List<Piece> prWhite = pr[(int) Piece.PieceColor.White];
        List<Piece> prBlack = pr[(int) Piece.PieceColor.Black];

        foreach (List<Piece> plist in new List<List<Piece>>(){prWhite, prBlack}) {
            foreach (Piece p in plist) {
                int[] PST_table = PST[p.get_piece_type()];

                total += -p.get_piece_color() * PST_table[p.get_board_position().Item1* board.CELL_SIZE + 
                                                        p.get_board_position().Item2];
            }
        }

        return total;
    }

    
    // returns the mobility score weighted
    private double get_mobility_score() {
        double total = 0.0;
        Dictionary<int, List<Piece>> pr = board.PieceRefs;
        
        List<Piece> prWhite = pr[(int) Piece.PieceColor.White];
        List<Piece> prBlack = pr[(int) Piece.PieceColor.Black];

        foreach (List<Piece> plist in new List<List<Piece>>(){prWhite, prBlack}) {
            foreach (Piece p in plist) {
                MoveManager mvm = new MoveManager(p, board);
                List<Move> ct = mvm.get_all_movement();

                total += -p.get_piece_color() * ct.Count * Mobility[p.get_piece_type()];
            }
        }

        return total;    
    }


    // returns the pawn score weighted by section
    private double get_pawn_score() {
        double total = 0.0;

        double WEIGHT_HANGING = 4.0;
        double WEIGHT_PASSED = 1.0;


        Dictionary<int, List<Piece>> pr = board.PieceRefs;
        
        List<Piece> prWhite = pr[(int) Piece.PieceColor.White];
        List<Piece> prBlack = pr[(int) Piece.PieceColor.Black];

        // Hanging Pawn scenario
        foreach (List<Piece> plist in new List<List<Piece>>(){prWhite, prBlack}) {
            foreach (Piece p in plist) {
                if (p.get_piece_type() == Piece.PieceType.Pawn && Helper_is_hanging(p)) {
                    total += p.get_piece_color() * WEIGHT_HANGING;
                }
            }
        }

        // Passed/Promotable Pawn scenario
        if (board.gm.get_num_moves() >= (int) gameStage.mid) {
            foreach (List<Piece> plist in new List<List<Piece>>(){prWhite, prBlack}) {
                foreach (Piece p in plist) {
                    if (p.get_piece_type() == Piece.PieceType.Pawn
                    && Helper_file_empty(p)) {
                        total += -p.get_piece_color() * WEIGHT_PASSED;
                    }
                }
            }
        }


        return total;
    }  

    // helper method for Pawn score, but can be used by other methods if needed in tuning
    private bool Helper_is_hanging(Piece p) {
        return p.get_allies(p.get_board_position(), board.BoardTiles).Count == 0
        && p.get_threats(p.get_board_position(), board.BoardTiles).Count > 0;
    }

    // returns whether the given piece p is contained within an empty rank in the direction to promotion
    private bool Helper_file_empty(Piece p) {
        if (p.get_piece_color() == (int) Piece.PieceColor.White) {
            for (int rank = p.get_board_position().Item1; rank > 0; rank-- ) {
                if (board.BoardTiles[rank, p.get_board_position().Item2] != null) {
                    return false;
                }
            }
        }
        else{
            for (int rank = p.get_board_position().Item1; rank < board.CELL_SIZE; rank++ ) {
                if (board.BoardTiles[rank, p.get_board_position().Item2] != null) {
                    return false;
                }
            }
        }

        return true;
    }


    // returns the bishop availability score (negative if less available)
    private double get_bishop_availability() {
        double total = 0.0;

        double PUNISH_FACTOR = 5.0;

        Dictionary<int, List<Piece>> pr = board.PieceRefs;
        
        List<Piece> prWhite = pr[(int) Piece.PieceColor.White];
        List<Piece> prBlack = pr[(int) Piece.PieceColor.Black];

        foreach (List<Piece> plist in new List<List<Piece>>(){prWhite, prBlack}) {
            int currbishop = -1;
            foreach (Piece p in plist) {
                if (p.get_piece_type() == Piece.PieceType.Bishop) {
                    currbishop = (p.get_board_position().Item1 + p.get_board_position().Item2) % 2;
                }
                else{
                    total += p.get_piece_color() * 
                   ((p.get_board_position().Item1 + p.get_board_position().Item2) % 2 == currbishop ? 1 : 0)
                   * PUNISH_FACTOR;
                }
            }
        }
        

        return total;
    }


    // returns the king safety score
    private double get_king_safety() {

        double total = 0.0;

        // piece shield. Less weighting for stronger blockers
        foreach (Piece p in new List<Piece>(){Board.White_King, Board.Black_King,}) {
            Tuple<int, int> wk = Board.White_King.get_board_position();
            if (board.BoardTiles[wk.Item1 - 1 * -p.get_piece_color(), wk.Item2] != null) {
                total += KingShield[board.BoardTiles[wk.Item1 - 1, wk.Item2].get_piece_type()]
                * -p.get_piece_color();     
            }
            if (board.BoardTiles[wk.Item1 - 1 * -p.get_piece_color(), wk.Item2 - 1] != null) {
                total += KingShield[board.BoardTiles[wk.Item1 - 1 * -p.get_piece_color(), wk.Item2 - 1].get_piece_type()]
                * -p.get_piece_color();     
            }
            if (board.BoardTiles[wk.Item1 - 1 * -p.get_piece_color(), wk.Item2 + 1] != null) {
                total += KingShield[board.BoardTiles[wk.Item1 - 1 * -p.get_piece_color(), wk.Item2 + 1].get_piece_type()]
                * -p.get_piece_color();     
            }
        }


        // to be added


        

        return total;
    
    }

    // returns the rook movement score
    private double get_rook_availability() {
        double total = 0;

        int PAWN_PENALTY = 12; // centipawns, -12 if same color, 12 if opposite

        List<Piece.PieceColor> pcol = new List<Piece.PieceColor>(){Piece.PieceColor.White, Piece.PieceColor.Black};

        foreach (Piece.PieceColor color in pcol) {
            foreach (Piece p in board.PieceRefs[(int) Piece.PieceColor.White])
            {
                if (p.get_piece_type() == Piece.PieceType.Rook) {
                    int file = p.get_board_position().Item1;
                    for (int rank = 0; rank < board.CELL_SIZE; rank++) {
                        if (board.BoardTiles[file, rank].get_piece_type() == Piece.PieceType.Pawn) {
                            if (board.BoardTiles[file, rank].get_piece_color() == p.get_piece_color()) {
                                total -= PAWN_PENALTY;
                            }
                            else{
                                total += PAWN_PENALTY;
                            }
                        }
                    }
                    
                }
            }            
        }

 
        return total;
    }
}

