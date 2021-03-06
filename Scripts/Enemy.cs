using Godot;
using System;

public class Enemy : Combatant
{
	//GAMEPLAY VARIABLES----------------------------------------------------------
	[Export]
	public int bounty = 10; //how much score do I add when killed
	public enum PathMode {STALKER, WANDERER, DEFENDER}; //which way I find my next position to move towards
    [Export]
	public float rangeThreshold = 6;
	[Export]
    public PathMode  closeRangeMode, longRangeMode;
	public PathMode currentMode;	
	public Arena arena;
	Navigation nav;
	public Spatial player;
	public Vector3 targetNavPos;
	Vector3 pathPos;
	Vector3[] path;
	AnimationPlayer ani;
	[Export]
	public float pathUpdateRate = 1;
	float updateTimer = 0;
	int pathPoint = 0;
	//COMPONENT VARIABLES---------------------------------------------------------
	[Export]
	public PackedScene deathExplosion = (PackedScene)ResourceLoader.Load("res://Prefabs/EnemyExplosion.tscn");
	
	Vector2 inputMovementVector = Vector2.Zero;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		arena = GetTree().Root.GetNode<Arena>("Spatial");
		ani = GetNode<AnimationPlayer>("AnimationPlayer");
		nav = GetNode<Navigation>("../Navigation");
		player = GetNode<Spatial>("../Player");
		targetNavPos = player.Translation;
		path = nav.GetSimplePath(Translation, targetNavPos, true);
		pathPos = Translation;
		currentMode = longRangeMode;
	}

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta)
	{
		PathUpdateTimer(delta);
		GetTarget();
		SetPathPos(targetNavPos);
		ProcessInput(delta);
		if (HP <= 0)
		{
			CPUParticles boom = (CPUParticles)deathExplosion.Instance();
			GetTree().Root.AddChild(boom);
			boom.Emitting = true;
			boom.Translation = Translation;
			
			arena.UpdateScore(bounty);
			QueueFree();
		}
	}
	protected void ProcessInput(float delta) //this is where all the AI happens
	{
		Transform camXform = GlobalTransform;
		dir = new Vector3();
		
		float distanceToPoint = Translation.DistanceTo(pathPos);
		float distanceToPlayer = Translation.DistanceTo(nav.GetClosestPoint(player.Translation));
		
		if (distanceToPlayer > rangeThreshold)
		{
			//GD.Print("outside range");
			currentMode = longRangeMode;
		}
		else
		{
			//GD.Print("within range");
			currentMode = closeRangeMode;
		}
		//the following line exists to rotate enemy wheels if it has any. I should find a way to interpolate the rotation to make the animation more natural.
		if (currentMode != PathMode.DEFENDER) LookAt(new Vector3(pathPos.x, Translation.y, pathPos.z), Vector3.Up);
		
		
		inputMovementVector = inputMovementVector.Normalized();
		dir += -camXform.basis.z.Normalized() * inputMovementVector.y;
		dir += camXform.basis.x.Normalized() * inputMovementVector.x;
		
		
	}
	public override void UpdateHealth(int delta)
	{
		HP += delta;
		if (delta > 0)
		{
			//this is here in case we ever consider something that heals enemies
		}
		else 
		{
			ani.Play("Hurt");//Add hit sound to this animation!
		}
		
	}
	void SetPathPos(Vector3 newPos) //This makes the movement vector follow the right point in the path array
	{
		
		pathPos = path[pathPoint];
		float distanceToPoint = Translation.DistanceTo(pathPos);
		if (distanceToPoint < 0.5f)
		{
			if (path.Length == 0)
			{
				GD.Print("Cannot find Path!");
				return;
			}
			//GD.Print("Target Reached");
			if (pathPoint < path.Length -1) //if it isn't on the final path point
			{
				pathPoint++; //increment point counter
				pathPos = path[pathPoint]; //set the movement target to the next point
			}
			else 
			{
				UpdatePath(newPos); //get a new array of path points
			}
		}
	}
	public void UpdatePath(Vector3 newPos)
	{
		pathPoint = 0; //reset counter
		path = nav.GetSimplePath(Translation, newPos, true); //generate new array of points
	}
	public void GetTarget()
	{
		switch (currentMode)
		{
			case PathMode.STALKER:
			targetNavPos = nav.GetClosestPoint(player.Translation);
			inputMovementVector = new Vector2 (0,1);
			break;
			case PathMode.WANDERER:
			targetNavPos = nav.GetClosestPoint(new Vector3((float)GD.RandRange(-30, 30), 2,(float)GD.RandRange(-30, 30)));
			inputMovementVector = new Vector2 (0,1);
			break;
			case PathMode.DEFENDER:
			targetNavPos = nav.GetClosestPoint(Translation);
			inputMovementVector = new Vector2 (0,0);
			break;
			default:
			targetNavPos = nav.GetClosestPoint(new Vector3((float)GD.RandRange(-30, 30), 2,(float)GD.RandRange(-30, 30)));
			inputMovementVector = new Vector2 (0,1);
			break;
		}
	}
	public void PathUpdateTimer(float delta)
	{
		updateTimer += delta;
		if (updateTimer >= pathUpdateRate) 
		{
			UpdatePath(targetNavPos);
			updateTimer = 0;
		}
			
	}
	//
}
