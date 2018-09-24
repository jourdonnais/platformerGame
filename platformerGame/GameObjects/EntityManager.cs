﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SFML.Graphics;
using SFML.System;

using platformerGame.GameObjects;
using platformerGame.Utilities;
using platformerGame.Map;
using platformerGame.Containers;
using tileLoader;

namespace platformerGame.GameObjects
{
    class EntityManager
    {
        const int ENTITY_GRID_SIZE = 64;
        const int ENTITY_OVERSCAN = 64;

        cGameScene pScene;

        List<cGameObject> allEntities;
        List<cGameObject> visibleEntites; // on screen

        Dictionary<Vector2i, List<cGameObject>> entityGrid;

        Vector2f worldSize;
        cPlayer player;

        private float cleanupTimer = 0.0f;

        public EntityManager(cGameScene scene, Vector2f world_size, cPlayer player)
        {
            this.pScene = scene;
            this.worldSize = world_size;
            this.player = player;
            this.allEntities = new List<cGameObject>();
            this.visibleEntites = new List<cGameObject>();
            this.entityGrid = new Dictionary<Vector2i, List<cGameObject>>();
        }

        public void InitLevelEntites(cMapData level)
        {
            // this.monsters.Clear();
            // this.allEntities.RemoveAll((cGameObject g) => g is cMonster );

            TmxMap map = level.GetTmxMap();
            TmxList<TmxObject> entityList = map.ObjectGroups["Entities"].Objects;
            foreach (var tmxEntity in entityList)
            {
                cMonster monster = new cMonster(this.pScene, new Vector2f((float)tmxEntity.X, (float)tmxEntity.Y));
                this.AddMonster(monster);
            }
        }

        private Vector2i calcGridPos(Vector2f world_pos)
        {
            int x = (int)world_pos.X / ENTITY_GRID_SIZE;
            int y = (int)world_pos.Y / ENTITY_GRID_SIZE;
            return new Vector2i(x, y);
        }

        private IEnumerable<cGameObject> getInGridRect(int startX, int startY, int endX, int endY)
        {
            var pos = new Vector2i();

            List<cGameObject> returner = new List<cGameObject>();

            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    pos.X = x;
                    pos.Y = y;

                    List<cGameObject> list;
                    if (entityGrid.TryGetValue(pos, out list))
                    {
                        returner.AddRange(list);
                    }
                }
            }

            return returner;
        }

        public IEnumerable<cGameObject> getEntitiesNearby(Vector2f pos)
        {
            Vector2i gridPos = this.calcGridPos(pos);
            return this.getInGridRect(gridPos.X - 1, gridPos.Y - 1, gridPos.X + 1, gridPos.Y + 1);
        }

        public IEnumerable<cGameObject> getEntitiesInArea(AABB area)
        {
            FloatRect rect = area.AsFloatRect();

            var overscan = ENTITY_OVERSCAN;
            var gridSize = ENTITY_GRID_SIZE;

            rect = new FloatRect(rect.Left - overscan, rect.Top - overscan,
                                 rect.Width + (overscan * 2), rect.Height + (overscan * 2));

            var startX = (int)rect.Left / gridSize;
            var startY = (int)rect.Top / gridSize;
            var width = (int)rect.Width / gridSize + 1;
            var height = (int)rect.Height / gridSize + 1;

            return this.getInGridRect(startX, startY, startX + width, startY + height);
        }

        public void AddPickup(cPickupAble p)
        {
            this.AddEntity(p);
        }

        public void AddMonster(cMonster m)
        {
            this.AddEntity(m);
        }

        public void AddBullet(cBullet b)
        {
            this.AddEntity(b);
        }

        public void AddEntity(cGameObject e)
        {
            e.GridCoordinate = this.calcGridPos(e.Bounds.center);
            this.allEntities.Add(e);
            this.GridAdd(e);

            /*
             if (e.InputInstance != null)
                inputEntities.Add(e);
            */
        }

        public void RemoveEntity(cGameObject e)
        {
            // e.Destroy();

            GridRemove(e);
            this.allEntities.Remove(e);

            /*
             if (e.InputInstance != null)
                inputEntities.Remove(e);
            */
        }

        public void Update(float step_time)
        {

            // bullet collision checks
            checkBulletVsEntityCollisions(step_time);

            // update all entites
            Vector2i newGridPos = new Vector2i(0, 0);

            int eCount = allEntities.Count;
            for (int i = 0; i < eCount; ++i)
            {
                cGameObject e = allEntities[i];

                if (e.isActive())
                {
                    e.Update(step_time);

                    var bounds = e.Bounds;
                    newGridPos = this.calcGridPos(bounds.center);

                    if (!e.GridCoordinate.Equals(newGridPos))
                    {
                        e.GridCoordinate = newGridPos;
                        GridAdd(e);
                    }
                }
                else
                {
                    allEntities.RemoveAt(i);
                    i--;
                    eCount = allEntities.Count;
                }

            }

            // check if can interact / use / pickup
            this.checkNearbyObjectsForPlayer();

            // cleanup grid
            cleanupTimer += step_time;
            if (cleanupTimer >= 60.0f)
            {
                entityGrid.RemoveAll(kv => kv.Value.Count == 0);
                cleanupTimer = 0;
            }
        }

        public void GridAdd(cGameObject e)
        {
            List<cGameObject> list;

            if (!entityGrid.TryGetValue(e.GridCoordinate, out list))
            {
                list = new List<cGameObject>();
                list.Add(e);
                entityGrid.Add(e.GridCoordinate, list);
                return;
            }

            list.Add(e);
        }

        public bool GridRemove(cGameObject e)
        {
            List<cGameObject> list;

            if (entityGrid.TryGetValue(e.GridCoordinate, out list))
                return list.Remove(e);

            return false;
        }

        /// <summary>
        /// Filters visible entites and performs view position calculation for the visible ones.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<cGameObject> filterVisibles(float alpha, AABB view_region)
        {
            foreach (var e in allEntities)
            {
                if (cCollision.OverlapAABB(e.Bounds, view_region))
                {
                    e.CalculateViewPos(alpha);
                    yield return e;
                }

            }
        }

        public void PreRender(float alpha, AABB view_region)
        {
            this.visibleEntites = this.filterVisibles(alpha, view_region).ToList<cGameObject>();
        }

        public void RenderBullets(RenderTarget target)
        {
            foreach (var b in this.visibleEntites.OfType<cBullet>())
            {
                b.Render(target);
            }
        }

        public void RenderPickups(RenderTarget target)
        {
            foreach (var p in this.visibleEntites.OfType<cPickupAble>())
            {
                p.Render(target);
            }
        }

        public void Render(RenderTarget target)
        {
            foreach (var m in this.visibleEntites.OfType<cMonster>())
            {
                m.Render(target);
            }
        }



        private cMonster getclosestMonsterColliding(IEnumerable<cMonster> possibleColliders, cBullet bul, Vector2f pos_by, float time)
        {
            // int index = -1;
            double prevDist = Double.MaxValue;
            double newDist = 0.0;
            cMonster returner = null;
            foreach (var mon in possibleColliders)
            {
                if (mon.IsKilled)
                    continue;

                // order by distance to find the closest
                if (cSatCollision.checkAndResolve(bul, mon, time, false))
                {
                    newDist = cAppMath.Vec2DistanceSqrt(mon.Bounds.center, pos_by);

                    if (newDist < prevDist)
                    {
                        prevDist = newDist;
                        returner = mon;
                    }
                }

            }

            return returner;
        }


        public void checkBulletVsEntityCollisions(float step_time)
        {
            // List<cMonster> collisionMonsters = new List<cMonster>();
            var bullets = this.allEntities.OfType<cBullet>();

            foreach (var bullet in bullets)
            {

                Vector2f intersection = new Vector2f(0.0f, 0.0f);

                var collisionMonsters = this.getEntitiesNearby(bullet.Position).OfType<cMonster>(); //treeMonsters.GetEntitiesAtPos(bullets[b].Position);


                cMonster monster = getclosestMonsterColliding(collisionMonsters, bullet, bullet.Position, step_time);

                if (monster != null)
                {
                    cCollision.resolveMonsterVsBullet(monster, bullet, intersection);
                }

            }
        }


        /// <summary>
        /// Player's pull distance must be smaller then cell_size in the spatial grid.
        /// </summary>
        public void checkNearbyObjectsForPlayer()
        {
            var pickups = this.getEntitiesNearby(this.player.Bounds.center).OfType<cPickupAble>();

            foreach (var p in pickups)
            {
                p.checkContactWithPlayer();
            }
        }
    }
}