using Godot;

public partial class UI3D : Node3D
{
    [Export]
    public SubViewport subViewport;

    [Export]
    public MeshInstance3D quad;

    [Export]
    public Area3D area;

    [Export]
    public CollisionShape3D shape;

    private Vector2 quadSize;
    private bool isMouseHeld = false;
    private bool isMouseInside = false;
    private Vector3? lastMousePosition3D;
    private Vector2 lastMousePosition2D = Vector2.Zero;

    public override void _Ready()
    {
        area.MouseEntered += MouseEnteredArea;

        CalculateSizes();
    }

    private void CalculateSizes()
    {
        var mesh = (QuadMesh)quad.Mesh;

        var material = new StandardMaterial3D
        {
            AlbedoTexture = subViewport.GetTexture(),
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };

        mesh.Material = material;

        mesh.Size = new Vector2(subViewport.Size.X / 1024f, subViewport.Size.Y / 1024f);

        (shape.Shape as BoxShape3D).Size = new Vector3(mesh.Size.X, mesh.Size.Y, .1f);
    }

    public override void _ExitTree()
    {
        area.MouseEntered -= MouseEnteredArea;
    }

    private void MouseEnteredArea()
    {
        isMouseInside = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        bool isMouseEvent = false;

        if (@event is InputEventMouse || @event is InputEventMouseButton || @event is InputEventMouseMotion)
        {
            isMouseEvent = true;
        }

        if (isMouseEvent && (isMouseInside || isMouseHeld))
        {
            HandleMouse((InputEventMouse)@event);
        }
        else if (!isMouseEvent)
        {
            subViewport.PushInput(@event);
        }
    }

    private void HandleMouse(InputEventMouse @event)
    {
        isMouseInside = FindMouse(@event.GlobalPosition, out Vector3 position);

        HandleMouseInPosition(@event, position);
    }

    public void HandleSynteticMouseMotion(Vector3 position)
    {
        var ev = new InputEventMouseMotion();

        isMouseInside = true;

        HandleMouseInPosition(ev, position);
    }

    public void HandleSynteticMouseClick(Vector3 position, bool pressed)
    {
        var ev = new InputEventMouseButton() { ButtonIndex = MouseButton.Left, Pressed = pressed };

        isMouseInside = true;

        HandleMouseInPosition(ev, position);
    }

    private void HandleMouseInPosition(InputEventMouse @event, Vector3 position)
    {
        quadSize = (quad.Mesh as QuadMesh).Size;

        if (@event is InputEventMouseButton)
        {
            isMouseHeld = @event.IsPressed();
        }

        Vector3 mousePosition3D;

        if (isMouseInside)
        {
            mousePosition3D = area.GlobalTransform.AffineInverse() * position;
            lastMousePosition3D = mousePosition3D;
        }
        else
        {
            if (lastMousePosition3D != null)
            {
                mousePosition3D = (Vector3)lastMousePosition3D;
            }
            else
            {
                mousePosition3D = Vector3.Zero;
            }
        }

        var mousePosition2D = new Vector2(mousePosition3D.X, -mousePosition3D.Y);

        mousePosition2D.X += quadSize.X / 2;
        mousePosition2D.Y += quadSize.Y / 2;

        mousePosition2D.X /= quadSize.X;
        mousePosition2D.Y /= quadSize.Y;

        mousePosition2D.X *= subViewport.Size.X;
        mousePosition2D.Y *= subViewport.Size.Y;

        @event.Position = mousePosition2D;
        @event.GlobalPosition = mousePosition2D;

        if (@event is InputEventMouseMotion)
        {
            (@event as InputEventMouseMotion).Relative = mousePosition2D - lastMousePosition2D;
        }

        lastMousePosition2D = mousePosition2D;

        subViewport.PushInput(@event);
    }

    private bool FindMouse(Vector2 globalPosition, out Vector3 position)
    {
        var camera = GetViewport().GetCamera3D();

        var from = camera.ProjectRayOrigin(globalPosition);
        var dist = FindFurtherDistanceTo(camera.Transform.Origin);
        var to = from + camera.ProjectRayNormal(globalPosition) * dist;

        var parameters = new PhysicsRayQueryParameters3D() { From = from, To = to, CollideWithAreas = true, CollisionMask = area.CollisionLayer, CollideWithBodies = false };

        var result = GetWorld3D().DirectSpaceState.IntersectRay(parameters);

        position = Vector3.Zero;

        if (result.Count > 0)
        {
            position = (Vector3)result["position"];

            return true;
        }
        else
        {
            return false;
        }
    }

    private float FindFurtherDistanceTo(Vector3 origin)
    {
        Vector3[] edges = new Vector3[] {
            area.ToGlobal(new Vector3(quadSize.X / 2, quadSize.Y / 2, 0)),
            area.ToGlobal(new Vector3(quadSize.X / 2, -quadSize.Y / 2, 0)),
            area.ToGlobal(new Vector3(-quadSize.X / 2, quadSize.Y / 2, 0)),
            area.ToGlobal(new Vector3(-quadSize.X / 2, -quadSize.Y / 2, 0)),
        };

        float farDistance = 0;

        foreach (var edge in edges)
        {
            var tempDistance = origin.DistanceTo(edge);

            if (tempDistance > farDistance)
            {
                farDistance = tempDistance;
            }
        }

        return farDistance;
    }
}
