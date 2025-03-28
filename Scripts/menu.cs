using Godot;
using System;

public partial class menu : Control
{
	Button play;
	Control menuItems;

	public GameManager scene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		scene = GetNode<GameManager>("GameManager"); // to "change scenes"

		menuItems = GetNode<Control>("MenuItems");		
		play = menuItems.GetNode<Button>("PlayButton");
		play.Connect("pressed", new Callable(this, MethodName.on_play_pressed));
	}

	public void on_play_pressed() {
		menuItems.Visible = false;
		scene.Visible = true;
		scene.GetNode<Camera2D>("Camera2D").Enabled = true;
	}
}
