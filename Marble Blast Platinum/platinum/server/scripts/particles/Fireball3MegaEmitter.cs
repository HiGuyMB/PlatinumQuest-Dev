//-----------------------------------------------------------------------------
// Copyright (c) 2021 The Platinum Team
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//-----------------------------------------------------------------------------


//--- Particle ---

datablock ParticleData(Fireball3MegaParticle) {
	dragCoefficient = "2.54902";
	windCoefficient = "0";
	gravityCoefficient = "-0.2";
	inheritedVelFactor = "0";
	constantAcceleration = "0";
	lifetimeMS = "480";
	lifetimeVarianceMS = "96";
	spinSpeed = "10";
	spinRandomMin = "2000";
	spinRandomMax = "2000";
	useInvAlpha = "0";
	animateTexture = "0";
	framesPerSec = "1";
	textureName = "platinum/data/particles/fireball_3";
	animTexName[0] = "platinum/data/particles/fireball_3";
	colors[0] = "0.843137 0.833333 0.843137 0.441176";
	colors[1] = "0.784314 0.813725 0.882353 1.000000";
	colors[2] = "0.843137 0.862745 0.892157 0.000000";
	colors[3] = "0.892157 0.911765 0.980392 0.000000";
	sizes[0] = "0.35";
	sizes[1] = "0.84";
	sizes[2] = "3.2";
	sizes[3] = "2.9";
	times[0] = "0";
	times[1] = "0.28";
	times[2] = "0.74";
	times[3] = "1";
	dragCoeffiecient = "1";
};

//--- Emitter ---

datablock ParticleEmitterData(Fireball3MegaEmitter) {
	className = "ParticleEmitterData";
	ejectionPeriodMS = "49";
	periodVarianceMS = "48";
	ejectionVelocity = "0";
	velocityVariance = "0";
	ejectionOffset = "0.2";
	thetaMin = "0";
	thetaMax = "90";
	phiReferenceVel = "0";
	phiVariance = "360";
	overrideAdvance = "0";
	orientParticles = "0";
	orientOnVelocity = "1";
	particles = "Fireball3MegaParticle";
	lifetimeMS = "0";
	lifetimeVarianceMS = "0";
	useEmitterSizes = "0";
	useEmitterColors = "0";
};
