﻿using SFML.System;
using platformerGame.Particles;

namespace platformerGame.GameCommands
{
    class comNormalBloodExplosion : cBaseGameCommand
    {
        EmissionInfo emission;
        public comNormalBloodExplosion(cGameScene scene, EmissionInfo emission) : base(scene)
        {
            this.emission = emission;
        }

        public override void Execute()
        {
            //scene.ParticleManager.Explosions.NormalBlood(emission);
        }
    }
}
