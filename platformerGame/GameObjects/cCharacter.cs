﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;

using platformerGame.Utilities;
using platformerGame.Map;
using platformerGame.App;

namespace platformerGame.GameObjects
{
    class cCharacter : cGameObject
    {
        public const uint FRAME_TIME = 60; //(uint)(1.0f / 0.0166f);

        protected RectangleShape shape;
        protected bool isOnGround;
        protected bool isJumpActive;
        protected bool isOnOnewWayPlatform;

        protected HorizontalFacing horizontalFacing;
        protected cSpriteStateController spriteControl;

        protected int health;
        protected cLight p_followLight = null;

        protected float walkSpeed;
        protected float maxWalkSpeed;


        public cCharacter(GameScene scene, Vector2f pos) : base(scene, pos)
        {
            spriteControl = new cSpriteStateController();
            initSprites();
            init();
        }

        protected virtual void initSprites()
        {
            spriteControl.Clear();
        }

        protected virtual void init()
        {
            Bounds = new AABB(0,0,1,1);
            Bounds.SetDims(new Vector2f(Constants.CHAR_COLLISON_RECT.Width, Constants.CHAR_COLLISON_RECT.Height));
            Bounds.SetPosByTopLeft(position);

            this.hitCollisionRect.SetDims(new Vector2f(32.0f, 32.0f));
            this.hitCollisionRect.SetPosByTopLeft(position);

            shape = new RectangleShape();
            shape.FillColor = Color.Green;
            shape.Size = new Vector2f(Bounds.dims.X, Bounds.dims.Y);

            isJumpActive = false;
            isOnGround = false;
            isOnOnewWayPlatform = false;

            horizontalFacing = HorizontalFacing.FACING_RIGHT;

            //must call, else not working
            spriteControl.ChangeState(this.GetSpriteState());

            this.health = 1;
        }

        public bool IsOnGround
        {
            get { return this.isOnGround; }
        }

        public bool IsOnOneWayPlatform
        {
            get { return isOnOnewWayPlatform; }
            set { isOnOnewWayPlatform = true; }
        }

        //hogy kívülről is leheseen offsettelni
        public void Move(float offset_x, float offset_y)
        {
            //lastPosition = position;
            position.X += offset_x;
            position.Y += offset_y;
        }

        protected virtual void updateX(float step_time, cWorld world)
        {
            acceleration.X = force.X;
            velocity.X += acceleration.X * step_time;

            if (acceleration.X < 0.0f)
            {
                velocity.X = AppMath.Max<float>(velocity.X, -this.maxWalkSpeed);
            }
            else if (acceleration.X > 0.0f)
            {
                velocity.X = AppMath.Min<float>(velocity.X, this.maxWalkSpeed);
            }
            else
            // if (isOnGround)
            {
                velocity.X = isOnGround ? velocity.X * Constants.GROUND_SLOW_DOWN_FACTOR
                    : velocity.X * Constants.AIR_SLOW_DOWN_FACTOR;
            }

            velocity.X = Math.Abs(velocity.X) <= 0.05f ? 0.0f : velocity.X;

            float delta = velocity.X * step_time;

            if (delta <= 0.0f)
            {
                float wallRightX;

                if (hasLeftWall2(world, delta, out wallRightX))
                {
                    position.X = wallRightX;
                    velocity.X = 0.0f;
                }
                else
                {
                    position.X += delta;
                }
            }
            else
            {
                float wallLeftX;
                if (hasRightWall2(world, delta, out wallLeftX))
                {
                    position.X = wallLeftX - Bounds.dims.X;
                    velocity.X = 0.0f;
                }
                else
                {
                    position.X += delta;
                }
            }
        }

        protected virtual void updateY(float step_time, cWorld world)
        {
            float gravity = (isJumpActive && velocity.Y < 0.0f) ? Constants.JUMP_GRAVITY : Constants.GRAVITY;

            force.Y += gravity;

            acceleration.Y = force.Y;

            velocity.Y += acceleration.Y * step_time;

            //velocity.Y = Math.Min(velocity.Y + gravity * step_time, Constants.MAX_Y_SPEED);
            velocity.Y = Math.Min(velocity.Y, Constants.MAX_Y_SPEED);

            float groundY;

            float delta = velocity.Y * step_time;

            if (delta >= 0.0f)
            {
                if (hasGround2(world, delta, out groundY))
                {
                    position.Y = groundY - Bounds.dims.Y;
                    isOnGround = true;
                    velocity.Y = 0.0f;

                    // bouncing
                    //velocity.Y = Math.Abs(velocity.Y) <= 65.0f ? 0.0f : -(velocity.Y * 0.8f);
                }
                else
                {
                    isOnGround = false;
                    position.Y += delta;
                }
            }
            else
            {
                float bottomY;
                if (hasCeiling(world, delta, out bottomY))
                {
                    position.Y = bottomY + 1.0f; //- Bounds.dims.Y;

                    velocity.Y = 0.0f;
                }
                else
                {
                    isOnGround = false;
                    position.Y += delta;
                }
            }
            //float delta = (velocity.Y * step_time);

        }

        protected virtual void updateMovement(float step_time)
        {
            cWorld world = pscene.World;

            lastPosition.X = position.X;
            lastPosition.Y = position.Y;

            updateX(step_time, world);
            updateY(step_time, world);

            Bounds.SetPosByTopLeft(position);
            this.hitCollisionRect.SetPosByTopLeft(position);

            this.force.X = 0.0f;
            this.force.Y = 0.0f;
        }

        public void StartMovingRight()
        {
            //acceleration.X = Constants.WALK_SPEED;
            this.AddForce(new Vector2f( this.walkSpeed,  0.0f));
            horizontalFacing = HorizontalFacing.FACING_RIGHT;
        }
        public void StartMovingLeft()
        {
            //acceleration.X = -Constants.WALK_SPEED;
            this.AddForce(new Vector2f( -this.walkSpeed, 0.0f));
            horizontalFacing = HorizontalFacing.FACING_LEFT;
            //m_HorizontalFacing = HorizontalFacing::FACING_LEFT;
        }
        public void StartJumping()
        {
            isJumpActive = true;

            if (isOnGround)
            {
                //m_pSpriteControl->ChangeState(Sprite_State(MotionType::JUMP, m_HorizontalFacing));
                velocity.Y = -Constants.JUMP_SPEED; // * cGame::StepTime;

                isOnGround = false;
            }
        }
        public void StopJumping()
        {
            isJumpActive = false;
        }
        public void StopMoving()
        {
            acceleration.X = 0.0f;
            //velocity.X = 0.0f;
        }

        public void StopMovingX()
        {
            // acceleration.X = 0.0f;
            velocity.X = 0.0f;
        }

        public cSpriteState GetSpriteState()
        {
            MotionType motion = MotionType.STAND;

            motion = isOnGround ? (Acceleration.X == 0.0f) ? MotionType.STAND : MotionType.WALK : (velocity.Y < 0.0f) ? MotionType.JUMP : MotionType.FALL;
            return new cSpriteState(motion, horizontalFacing);

        }

        public override void Update(float step_time)
        {

            updateMovement(step_time);
            spriteControl.Update(this.GetSpriteState());
        }

        public override void Render(RenderTarget destination)
        {
            //viewPosition = cAppMath.Interpolate(position, lastPosition, alpha);
            this.spriteControl.Render(destination, viewPosition);
        }

        protected bool hasCeiling(cWorld world, float delta, out float bottomY)
        {

            bottomY = 0.0f;

            float predictedPosY = position.Y + delta;

            Vector2f oldTopLeft = new Vector2f(position.X, position.Y);
            Vector2f newTopLeft = new Vector2f(position.X, predictedPosY);
            Vector2f newTopRight = new Vector2f(newTopLeft.X + Bounds.dims.X - 2.0f, newTopLeft.Y);

            int endY = world.ToMapPos(oldTopLeft).Y; //mMap.GetMapTileYAtPoint(newBottomLeft.y);
            int begY = Math.Min(world.ToMapPos(newTopLeft).Y, endY);


            int dist = Math.Max(Math.Abs(endY - begY), 1);

            int tileIndexX;

            for (int tileIndexY = begY; tileIndexY <= endY; ++tileIndexY)
            {
                var topLeft = AppMath.Interpolate(newTopLeft, oldTopLeft, (float)Math.Abs(endY - tileIndexY) / dist);
                var topRight = new Vector2f(topLeft.X + Bounds.dims.X - 1, topLeft.Y);

                for (var checkedTile = topLeft; ; checkedTile.X += Constants.TILE_SIZE)
                {
                    checkedTile.X = Math.Min(checkedTile.X, topRight.X);

                    tileIndexX = world.ToMapPos(checkedTile).X;

                    if (world.CurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).Type == TileType.WALL)
                    {
                        bottomY = (((float)tileIndexY) * Constants.TILE_SIZE + world.WorldBounds.topLeft.Y + Constants.TILE_SIZE);
                        return true;
                    }

                    if (checkedTile.X >= topRight.X)
                        break;
                }
            }

            return false;
        }

        protected bool hasGround(cWorld world, float delta, out float groundY)
        {
            /*var oldCenter = lastPosition;
            var center = position;*/

            groundY = 0.0f;

            float predictedPosY = position.Y + delta;
            /*
            Vector2f up = new Vector2f(0, -1);
            Vector2f bottom = new Vector2f(0, 1);
            Vector2f left = new Vector2f(-1, 0);
            Vector2f right = new Vector2f(1, 0);*/

            Vector2f oldBottomLeft = new Vector2f(position.X, position.Y + Bounds.dims.Y);
            Vector2f newBottomLeft = new Vector2f(position.X, predictedPosY + Bounds.dims.Y);
            Vector2f newBottomRight = new Vector2f(newBottomLeft.X + Bounds.dims.X , newBottomLeft.Y);

            int endY = world.ToMapPos(newBottomLeft).Y; //mMap.GetMapTileYAtPoint(newBottomLeft.y);
            int begY = Math.Max(world.ToMapPos(oldBottomLeft).Y - 1, endY);

            int dist = Math.Max(Math.Abs(endY - begY), 1);

            int tileIndexX;

            for (int tileIndexY = begY; tileIndexY <= endY; ++tileIndexY)
            {
                var bottomLeft = AppMath.Interpolate(newBottomLeft, oldBottomLeft, (float)Math.Abs(endY - tileIndexY) / dist);
                var bottomRight = new Vector2f(bottomLeft.X + Bounds.dims.X - 1.0f, bottomLeft.Y); // -1, -2.0f

                for (var checkedTile = bottomLeft; ; checkedTile.X += Constants.TILE_SIZE)
                {
                    checkedTile.X = Math.Min(checkedTile.X, bottomRight.X);

                    tileIndexX = world.ToMapPos(checkedTile).X;

                    //world.GetCurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).PlayerCollidable = true;

                    groundY = (int)((float)tileIndexY * Constants.TILE_SIZE + world.WorldBounds.topLeft.Y);

                    TileType tile = world.CurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).Type;
                    if (tile == TileType.WALL)
                    {
                        isOnOnewWayPlatform = false;
                        return true;
                    }
                    else if (tile == TileType.ONEWAY_PLATFORM && position.Y <= groundY - Bounds.dims.Y)
                    {
                        isOnOnewWayPlatform = true;
                        return true;
                    }

                    if (checkedTile.X >= bottomRight.X)
                    {
                        /*if(isOnOnewWayPlatform)
                            return true;*/
                        break;
                    }
                }
            }

            return false;
        }

        // local function for check
        protected bool tileCollision(int tileX, int tileY, out float grY)
        {
            var world = this.Scene.World;

            TileType tile = world.CurrentLevel.GetTileAtXY(tileX, tileY).Type;
            grY = (int)(tileY * Constants.TILE_SIZE + world.WorldBounds.topLeft.Y);
            if (tile == TileType.WALL)
            {
                isOnOnewWayPlatform = false;
                return true;
            }
            else if (tile == TileType.ONEWAY_PLATFORM && position.Y <= grY - Bounds.dims.Y)
            {
                isOnOnewWayPlatform = true;
                return true;
            }
            return false;

        }

        protected bool hasGround2(cWorld world, float delta, out float groundY)
        {
            groundY = 0.0f;

            float predictedPosY = position.Y + delta;

            Vector2f oldBottomRight = new Vector2f(position.X + Bounds.halfDims.X, position.Y + Bounds.dims.Y);

            Vector2f oldBottomLeft = new Vector2f(position.X, position.Y + Bounds.dims.Y);
            Vector2f newBottomLeft = new Vector2f(oldBottomLeft.X, predictedPosY + Bounds.dims.Y);
            Vector2f newBottomRight = new Vector2f(newBottomLeft.X + Bounds.halfDims.X, newBottomLeft.Y);

            // int tileBeginX = Math.Min(world.ToMapPos(oldBottomLeft).X, tileEndX);
           
            int tileBeginX = world.ToMapPos(oldBottomLeft).X-1;
            int tileEndX = world.ToMapPos(newBottomRight).X+1;


            int tileBeginY = world.ToMapPos(oldBottomLeft).Y-1;
            int tileEndY = world.ToMapPos(newBottomLeft).Y+1;

            

           
                if (this.velocity.X < 0.0f)
                {
                    for (int tileIndexY = tileBeginY; tileIndexY <= tileEndY; ++tileIndexY)
                    {
                        for (int tileIndexX = tileBeginX; tileIndexX < tileEndX; ++tileIndexX)
                        {
                            if (tileCollision(tileIndexX, tileIndexY, out groundY))
                            {
                                return true;
                            }
                        }
                    }
                }
                else
                {
                    for (int tileIndexY = tileBeginY; tileIndexY <= tileEndY; ++tileIndexY)
                    {
                        for (int tileIndexX = tileEndX; tileIndexX > tileBeginX; --tileIndexX)
                        {
                            if (tileCollision(tileIndexX, tileIndexY, out groundY))
                            {
                                return true;
                            }
                        }
                    }
                }

                
                    //if (world.CurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).Type == TileType.WALL)
                    /*world.isRectOnWall()*/
                    /*
                    {
                        //world.GetCurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).PlayerCollidable = true;

                        world.CurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).PlayerCollidable = true;
                        groundY = (int)(tileIndexY * Constants.TILE_SIZE + world.WorldBounds.topLeft.Y);
                        return true;
                    }
                    */
                
            

            return false;
        }

        /// <summary>
        /// balról jobbra, alulról felfelé
        /// </summary>
        /// <param name="world"></param>
        /// <param name="delta"></param>
        /// <param name="wallRightX"></param>
        /// <returns></returns>
        protected bool hasLeftWall2(cWorld world, float delta, out float wallRightX)
        {
            wallRightX = 0.0f;

            float predictedPosX = position.X + delta;

            Vector2f oldTopLeft = new Vector2f(position.X, position.Y);
            Vector2f newTopLeft = new Vector2f(predictedPosX, position.Y);
            Vector2f newBottomLeft = new Vector2f(newTopLeft.X, newTopLeft.Y + Bounds.dims.Y-2.0f);

            int tileEndX = world.ToMapPos(newTopLeft).X-1;
            int tileBeginX = Math.Max(world.ToMapPos(oldTopLeft).X, tileEndX);

            int tileBeginY = world.ToMapPos(newTopLeft).Y;
            int tileEndY = world.ToMapPos(newBottomLeft).Y;

            for (int tileIndexX = tileBeginX; tileIndexX > tileEndX; --tileIndexX)
            {
                for (int tileIndexY = tileBeginY; tileIndexY <= tileEndY; ++tileIndexY)
                {
                    //world.GetCurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).PlayerCollidable = false;
                    if (world.CurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).Type == TileType.WALL)
                    /*world.isRectOnWall()*/
                    {
                        //world.GetCurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).PlayerCollidable = true;
                        wallRightX = (int)(tileIndexX * Constants.TILE_SIZE + Constants.TILE_SIZE); // + world.WorldBounds.topLeft.X
                        return true;
                    }
                }
            }

            return false;
        }
        

        protected bool hasRightWall2(cWorld world, float delta, out float wallLeftX)
        {
            wallLeftX = 0.0f;

            float predictedPosX = position.X + Bounds.dims.X + delta;

            Vector2f oldTopRight = new Vector2f(position.X + Bounds.dims.X, position.Y);
            Vector2f newTopRight = new Vector2f(predictedPosX, position.Y);
            Vector2f newBottomRight = new Vector2f(newTopRight.X, newTopRight.Y + Bounds.dims.Y-2.0f);

            int tileEndX = world.ToMapPos(newTopRight).X + 1;
            int tileBeginX = Math.Min(world.ToMapPos(oldTopRight).X, tileEndX);

            int tileBeginY = world.ToMapPos(newTopRight).Y;
            int tileEndY = world.ToMapPos(newBottomRight).Y; // changed to handle right walking between walls when falling

            for (int tileIndexX = tileBeginX; tileIndexX < tileEndX; ++tileIndexX)
            {
                for (int tileIndexY = tileBeginY; tileIndexY <= tileEndY; ++tileIndexY)
                {
                    //world.GetCurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).PlayerCollidable = false;
                    if (world.CurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).Type == TileType.WALL)
                    /*world.isRectOnWall()*/
                    {
                        //world.GetCurrentLevel.GetTileAtXY(tileIndexX, tileIndexY).PlayerCollidable = true;
                        wallLeftX = (int)(tileIndexX * Constants.TILE_SIZE); // + world.WorldBounds.topLeft.X
                        return true;
                    }
                }
            }

            return false;
        }

        public virtual void Hit(int amount, cGameObject entity_by)
        {
            this.health -= amount;
        }

        public virtual void MeleeHit(int amount, cGameObject entity_by)
        {
            this.Hit(amount, entity_by);
            // Vector2f towardsMe = AppMath.Vec2NormalizeReturn(this.HitCollisionRect.center - entity_by.HitCollisionRect.center);
            // this.force = towardsMe * 50000;
            
        }

        public int Health
        {
            get { return health; }
            set { health = value; }
        }

        ~cCharacter()
        {
            spriteControl.Clear();
        }

    }
}
