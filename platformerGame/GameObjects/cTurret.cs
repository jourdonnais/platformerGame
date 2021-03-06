﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SFML.Graphics;
using SFML.System;
using platformerGame.Utilities;
using platformerGame.GameObjects.PickupInfo;
using platformerGame.Rendering;
using platformerGame.App;
using platformerGame.Inventory.Weapons;

namespace platformerGame.GameObjects
{
    class cTurret : cGameObject
    {
        // Sprite sprite;
        const double SPOT_DISTANCE = 300.0;
        
        cMachineGun gun;
        Vector2f gunFacingDirection;

        RectangleShape shape;

        cLight light;

        public cTurret() : base()
        {

        }

        public cTurret(GameScene scene, Vector2f pos) : base(scene, pos)
        {
            this.Bounds.SetDims(new Vector2f(16, 16));
            this.Bounds.SetPosByCenter(pos);

            gun = new cMachineGun(this, 6, "turret-bullet");

            gunFacingDirection = new Vector2f(0.0f, -1.0f);

            shape = new RectangleShape(new Vector2f(Bounds.dims.X, Bounds.dims.Y));
            shape.Origin = new Vector2f(Bounds.halfDims.X, Bounds.halfDims.Y);
            shape.FillColor = Color.Green;
            shape.Position = new Vector2f(pos.X, pos.Y);
            shape.Scale = new Vector2f(1.0f, 1.0f);

            light = new cLight();
            light.Pos = this.Bounds.center;
            light.Radius = 100.0f;
            light.LinearizeFactor = 0.8f;
            light.Bleed = 8.0f;
            light.Color = new Color(20, 184, 87);
            this.Scene.LightMap.AddStaticLight(light);
        }

        public override void Update(float step_time)
        {
            double dist = AppMath.Vec2Distance(Scene.Player.Bounds.center, this.Bounds.center);
            if(dist <= SPOT_DISTANCE)
            {
                Vector2f toTarget = AppMath.Vec2NormalizeReturn((Scene.Player.Bounds.center + Scene.Player.Velocity) - this.Bounds.center);

                
                float ang = AppMath.GetAngleBetwenVecs(toTarget, gunFacingDirection);
                Quaternion q = Quaternion.CreateFromAxisAngle(new Vector3f(toTarget.X, toTarget.Y, 0.0f), ang);
                Quaternion q2 = Quaternion.CreateFromAxisAngle(new Vector3f(gunFacingDirection.X, gunFacingDirection.Y, 0.0f), ang);
                q = Quaternion.Slerp(q, q2, ang);
                
                gunFacingDirection = new Vector2f(q.X, q.Y); //  toTarget;
                gun.Fire(Scene.Player.Bounds.center /*+ Scene.Player.Velocity * step_time*/);
            }
        }

        public override void Kill(cGameObject by)
        {
            Scene.LightMap.remove(this.light);
        }

        public override bool isActive()
        {
            return base.isActive();
        }

        public override void Render(RenderTarget destination)
        {
           destination.Draw(shape, new RenderStates(BlendMode.Add));

           DrawingBase.DrawLine(destination, this.Bounds.center, this.Bounds.center + gunFacingDirection * 40.0f, Color.Yellow, BlendMode.Alpha);
           // DrawingBase.DrawRectangleShape(destination, this.Bounds, new Color(240,140,160), BlendMode.Alpha);
        }
    }
}
