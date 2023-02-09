using Godot;

public partial class Hand : XRController3D
{
	[Export]
	public RayCast3D raycastCursor;

	[Export]
	public MeshInstance3D raycastVisualizer;

	public override void _Ready()
	{
		ButtonPressed += HandleButtonPress;

		raycastVisualizer.Visible = false;
    }

	public UI3D hoveredUi;

	public override void _PhysicsProcess(double delta)
	{
		if (raycastCursor.IsColliding())
		{
			HandleRaycastCollision();
		}
		else if (hoveredUi != null || raycastVisualizer.Visible)
		{
			HideRaycastVisualizer();
		}
	}

	private void HideRaycastVisualizer()
	{
		raycastVisualizer.Visible = false;
        hoveredUi = null;
	}

	private void HandleRaycastCollision()
	{
		if (raycastCursor.GetCollider() is not Area3D area)
		{
			HideRaycastVisualizer();
			return;
		}

		if (area.GetParent() is not MeshInstance3D mesh)
		{
			HideRaycastVisualizer();
			return;
		}

		if (mesh.GetParent() is not UI3D ui)
		{
			HideRaycastVisualizer();
			return;
		}

		raycastVisualizer.Visible = true;
		hoveredUi = ui;

		var point = raycastCursor.GetCollisionPoint();

		ui.HandleSynteticMouseMotion(point);
	}

	private void HandleButtonPress(string name)
	{
		if (name == "trigger_click")
		{
			if (hoveredUi != null)
			{
				HandleUIClick();
			}
		}
	}

	private void HandleUIClick()
	{
		var point = raycastCursor.GetCollisionPoint();

		hoveredUi.HandleSynteticMouseClick(point, true);
		hoveredUi.HandleSynteticMouseClick(point, false);
	}
}
