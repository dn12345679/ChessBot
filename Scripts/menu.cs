using Godot;

public partial class menu : Control
{
	Button play;
	Button play_bot;
	Control menuItems;

	public GameManager scene;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		scene = GetNode<GameManager>("GameManager"); // to "change scenes"

		menuItems = GetNode<Control>("MenuItems");		

		play = menuItems.GetNode<Button>("PlayButton");
		play.Connect("pressed", new Callable(this, MethodName.on_play_pressed));

		play_bot = menuItems.GetNode<Button>("AIPlayButton");
		play_bot.Connect("pressed", new Callable(this, MethodName.on_play_ai_pressed));
	}

	public void on_play_pressed() {
		init_scene();
		scene.set_players("Player", "Player");
	}
	public void on_play_ai_pressed() {
		init_scene();
		scene.set_players("Player", "AI");
	}

	private void init_scene() {
		menuItems.Visible = false;
		scene.Visible = true;
		scene.GetNode<Camera2D>("Camera2D").Enabled = true;
	}
}
