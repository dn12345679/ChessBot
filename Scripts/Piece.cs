using Godot;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

public partial class Piece : Node2D
{
	public enum PieceType
	{
		Default = 0, 
		Pawn = 1,
		Rook = 2, 
		Knight = 3, 
		Bishop = 4, 
		Queen = 5, 
		King = 6
	}
	public enum PieceColor
	{
		White = 1,
		Black = -1, 
		Default = 0
	}


	private PieceType ptype = PieceType.Default;
	private PieceColor pcolor = PieceColor.Default;
	private Vector2 PiecePosition = Vector2.Zero;


	private TextureRect image = null;
	
	public Piece(Vector2 pPos, int PieceType, int PieceColor) {
		this.PiecePosition = pPos;

		foreach (PieceType pval in Enum.GetValues(typeof(PieceType))) {
			if ((int) pval == PieceType) {
				ptype = pval;
			}
		}
		pcolor = PieceColor == 1 ? Piece.PieceColor.White : Piece.PieceColor.Black;
		SetIcon(); // adds images to the piece
		GlobalPosition = pPos;
	}

	/*
	Part of the initialization process of a Piece
	Using the default values set in the constructor, adds an image using the default sprite sheet
	*/
	public void SetIcon() {
		Texture2D Icons = ResourceLoader.Load<Texture2D>("res://Assets/ChessPieces.png");
		AtlasTexture Atlas = new AtlasTexture();
		TextureRect sprite = new TextureRect();

		Atlas.Atlas = Icons;
		sprite.Texture = Atlas;
		
		
		int row = 0;
		if (pcolor == PieceColor.Black) {
			row = 1;
		}
		int col = 0;
		
		switch (ptype)
		{
			case PieceType.Default:
				break;

			case PieceType.Pawn:
				col = 5;
				break;
			case PieceType.Rook:
				col = 4;
				break;
			case PieceType.Knight:
				col = 3;
				break;
				
			case PieceType.Bishop:
				col = 2;
				break;

			case PieceType.Queen:
				col = 1;
				break;

			case PieceType.King:
				col = 0;
				break;
		}
		Rect2 region = new Rect2(new Vector2(col * 2560/6, row * 2560/6), new Vector2(2560/6, 2560/6));
		sprite.Scale = new Vector2(0.0775f, 0.0775f);
		Atlas.Region = region;
		
		image = sprite;
		AddChild(sprite);

	}
}
