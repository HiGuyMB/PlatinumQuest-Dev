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

// reloader
function autoreload(%file) {
	cancel($autoreload[%file]);
	$autoreload[%file] = schedule(1000, 0, autoreload, %file);
	%crc = getFileCRC(%file);
	if (%crc !$= $crc[%file]) {
		$crc[%file] = %crc;
		exec(%file);
	}
}
autoreload($Con::File);


function debugSendChat(%message) {
	%count = ClientGroup.getCount();
	for (%i = 0; %i < %count; %i ++) {
		%client = ClientGroup.getObject(%i);
		if (!%client.isSuperAdmin)
			continue;
		commandToClient(%client, 'PrivateMessage', LBChatColor("whispermsg") @ "[Cycle Debug] " @ %message);
	}
	echo("[Cycle Debug] " @ %message);
}

function cycleSendChat(%message) {
	serverSendChat(LBChatColor("record") @ "[Cycle] " @ %message);
	echo("[Cycle] " @ %message);
}


//States of the cycle server state machine

// 0 - At the level select, players get to vote
// 1 - Loading the chosen level
// 2 - Waiting at the pregame screen for players to press ready
// 3 - Playing the level
$Cycle::State[0] = "select";
$Cycle::State[1] = "load";
$Cycle::State[2] = "ready";
$Cycle::State[3] = "play";

$Cycle::Time[0] = 1000*60;
$Cycle::Time[2] = 1000*20;

$Cycle::Pause[1] = 1000*10;
$Cycle::Pause[3] = 1000*30;

function startCycleServer() {
	activatePackage(CycleServer);

	$Server::Cycle = true;
	$Server::Controllable = false;
	$Cycle::State = -1;

	cycle0start();
}

function cycleChatCommand(%client, %message) {
	if (getWord(%message, 0) $= "/cycle") {
		return true;
	}
}

//TODO:
// What happens if people join midway -- try to include them
// What happens if everyone leaves -- Stop mission, return to state 0
// At state 0, if there is nobody, don't start the countdown until somebody joins
// What if nobody votes -- pick a random map

//-----------------------------------------------------------------------------
// State 0: Level Selection
//-----------------------------------------------------------------------------

function cycle0Start() {
	$Cycle::State = 0;
	debugSendChat("Now state " @ $Cycle::State);

	//Set everybody to host so they can choose missions
	for (%i = 0; %i < ClientGroup.getCount(); %i ++) {
		%client = ClientGroup.getObject(%i);
		%client.cycle0Start();
	}

	// Find random missions from the server to add to the pool
	%count = 2;
	$Cycle::ServerPicks = %count;
	for (%i = 0; %i < %count; %i ++) {
		$Cycle::ServerPick[%i] = getRandomHuntMap().file;
		debugSendChat("Server pick " @ %i @ " is " @ $Cycle::ServerPick[%i]);
	}

	cycleCountdown("Voting ends", $Cycle::Time[0], cycle0Finish);
}

function GameConnection::cycle0Start(%this) {
	commandToClient(%this, 'HostStatus', 1);
	%this.sendChat(LBChatColor("record") @ "[Cycle] Select a mission and press Preload to vote for it!");

	%this.cycle0Vote = "";
}

function cycle0Switch(%client, %file, %game, %difficulty, %forceMode) {
	//Probably nothing
	debugSendChat(%client.getDisplayName() @ " switches to " @ %file);
}

function cycle0Vote(%client, %file) {
	debugSendChat(%file);
	if (isScriptFile(%file)) {

		//Tell them to stop picking
		%client.setHost(false);

		%info = getMissionInfo(%file);

		//Then count their vote
		debugSendChat(%client.getDisplayName() @ " votes for " @ %file);
		cycleSendChat(%client.getDisplayName() @ " votes for " @ %info.name);

		%client.cycle0Vote = %file;
		commandToClient(%client, 'HostStatus', 0);

		cycle0Check();
	} else {
		debugSendChat(%client.getDisplayName() @ " fails to vote for " @ %file);
		%client.sendChat(LBChatColor("record") @ "[Cycle] Server does not have that mission! Pick another one.");
	}
}

function cycle0Check() {
	for (%i = 0; %i < ClientGroup.getCount(); %i ++) {
		%client = ClientGroup.getObject(%i);

		if (%client.cycle0Vote $= "") {
			return;
		}
	}

	cycle0Finish();
}

function cycle0Finish() {
	cycleCountdownCancel();

	if (ClientGroup.getCount() == 0) {
		cycle0start();
		return;
	}

	cycleSendChat("========================");
	cycleSendChat("Votes:");
	cycleSendChat("========================");

	%levels = Array("Cycle0Levels");

	// Accumulate level votes
	for (%i = 0; %i < ClientGroup.getCount(); %i ++) {
		%client = ClientGroup.getObject(%i);
		if (%client.cycle0Vote $= "") {
			continue;
		}

		if (isScriptFile(%client.cycle0Vote)) {
			%levels.addEntry(%client.cycle0Vote);
			cycleSendChat(%client.getDisplayName() @ ": " @ getMissionInfo(%client.cycle0Vote).name);
		}
	}
	for (%i = 0; %i < $Cycle::ServerPicks; %i ++) {
		%levels.addEntry($Cycle::ServerPick[%i]);
		cycleSendChat("Server Random Pick " @ (%i + 1) @ ": " @ getMissionInfo($Cycle::ServerPick[%i]).name);
	}

	if (%levels.getSize() == 0) {
		cycleSendChat("No votes! Using server picks...");
	}

	//Pick a random one
	%file = %levels.getEntry(getRandom(0, %levels.getSize() - 1));
	cycleSendChat("Picked " @ getMissionInfo(%file).name);

	%levels.delete();

	//Play it!
	cycle1Start(%file);
}

//-----------------------------------------------------------------------------
// State 1: Loading
//-----------------------------------------------------------------------------

function cycle1Start(%file) {
	$Cycle::State = 1;
	debugSendChat("Now state " @ $Cycle::State);
	for (%i = 0; %i < ClientGroup.getCount(); %i ++) {
		%client = ClientGroup.getObject(%i);
		%client.cycle1Start();
	}

	%info = getMissionInfo(%file, true);

	$Cycle::LoadFailed = false;

	cycleSendChat("Playing " @ %info @ " " @ %file);

	serverSetMission(%file, %info.game, %info.type, "");
	serverLoadMission(%file);

	if ($Cycle::LoadFailed || !$loadingMission) {
		lobbyReturn();
		cycle0Start();
	}
}

function GameConnection::cycle1Start(%this) {
	commandToClient(%this, 'HostStatus', 0);
}

function cycle1CheckLoad() {
	if ($Server::Loaded) {
		cycle1Finish();
	}
}

function cycle1Finish() {
	cycleCountdown("Starting", $Cycle::Pause[1], cycle2Start);
}

//-----------------------------------------------------------------------------
// State 2: Pregame
//-----------------------------------------------------------------------------

function cycle2Start(%file) {
	$Cycle::State = 2;
	debugSendChat("Now state " @ $Cycle::State);
	serverEnterGame();

	cycleCountdown("Playing", $Cycle::Time[2], cycle2Finish);
}

function cycle2Ready(%client) {
	if ($MP::ReadyCount == getRealPlayerCount()) {
		cycle2Finish();
	}
}

function cycle2Finish() {
	cycleCountdownCancel();
	cycle3Start();
}

//-----------------------------------------------------------------------------
// State 3: Playing
//-----------------------------------------------------------------------------

function cycle3Start(%file) {
	$Cycle::State = 3;
	debugSendChat("Now state " @ $Cycle::State);
	serverPreGamePlay(true);
}

function cycle3Finish() {
	cycleCountdown("Returning", $Cycle::Pause[1], cycle3Cleanup);
}

function cycle3Cleanup() {
	cycleCountdownCancel();

	lobbyReturn();
	if (ClientGroup.getCount() == 0) {
		$Cycle::State = -1;
		debugSendChat("Now state " @ $Cycle::State);
	} else {
		cycle0Start();
	}

	commandToAll('LobbyReturned');
}

function cycle3CheckCleanup() {
	if (ClientGroup.getCount() == 0) {
		cycle3Cleanup();
	}
}

//-----------------------------------------------------------------------------
// Override package
//-----------------------------------------------------------------------------

package CycleServer {
	function checkAllClientsLoaded() {
		Parent::checkAllClientsLoaded();
		if ($Cycle::State == 1) {
			cycle1CheckLoad();
		}
	}
	function serverCmdSetMission(%client, %file, %game, %difficulty, %forceMode) {
		if ($Cycle::State == 0) {
			cycle0Switch(%client, %file, %game, %difficulty, %forceMode);
		}
	}
	function serverCmdLoadMission(%client, %file) {
		if ($Cycle::State == 0) {
			cycle0Vote(%client, %file);
		}
	}
	function runServerChatCommand(%client, %message) {
		if (cycleChatCommand(%client, %message))
			return true;
		return Parent::runServerChatCommand(%client, %message);
	}
	function GameConnection::finishConnect(%client) {
		Parent::finishConnect(%client);

		//Gotta get them up to speed
		switch ($Cycle::State) {
		case -1:
			cycle0Start();
		case 0:
			%client.cycle0Start();
		case 1:
			%client.cycle1Start();
		}
	}
	function GameConnection::onDrop(%client, %reason) {
		Parent::onDrop(%client, %reason);
		echo(ClientGroup.getCount());
		ClientGroup.listObjects();
		onNextFrame(cycle3CheckCleanup);
	}
	function GameConnection::setHost(%client, %host) {
		// No
	}

	function serverCmdReady(%client, %ready) {
		Parent::serverCmdReady(%client, %ready);
		updateReadyUserList();
		if ($Cycle::State == 2) {
			cycle2Ready(%client);
		}
	}
	function endGameSetup() {
		Parent::endGameSetup();
		cycle3Finish();
	}

	function onMissionLoadFailed() {
		Parent::onMissionLoadFailed();
		$Cycle::LoadFailed = true;
	}
};

//-----------------------------------------------------------------------------
// Helper functions
//-----------------------------------------------------------------------------

function cycleCountdown(%name, %time, %function) {
	//Cancel previous
	cycleCountdownCancel();

	$Cycle::Countdowns = -1; //pls
	$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time, 0, %function);

	cycleSendChat(cycleFormatCountdown(%time, %name));
	$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 1000, 0, cycleSendChat, cycleFormatCountdown(1000, %name));
	$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 2000, 0, cycleSendChat, cycleFormatCountdown(2000, %name));
	$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 3000, 0, cycleSendChat, cycleFormatCountdown(3000, %name));
	$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 4000, 0, cycleSendChat, cycleFormatCountdown(4000, %name));
	if (%time > 5000) {
		$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 5000, 0, cycleSendChat, cycleFormatCountdown(5000, %name));
	}
	if (%time > 10000) {
		$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 10000, 0, cycleSendChat, cycleFormatCountdown(10000, %name));
	}
	if (%time > 30000) {
		$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 30000, 0, cycleSendChat, cycleFormatCountdown(30000, %name));
	}
	if (%time > 60000) {
		$Cycle::Countdown[$Cycle::Countdowns++] = schedule(%time - 60000, 0, cycleSendChat, cycleFormatCountdown(60000, %name));
	}
}

function cycleCountdownCancel() {
	for (%i = 0; %i <= $Cycle::Countdowns; %i ++) {
		cancel($Cycle::Countdown[%i]);
	}
}

function cycleFormatCountdown(%time, %name) {
	return %name @ " in " @ (%time / 1000) @ "...";
}

function collectMissions(%ml, %game, %difficulty, %collection) {
	%list = %ml.getMissionList(%game, %difficulty);
	if (!isObject(%list)) {
		%ml.buildMissionList(%game, %difficulty);
	}
	if (!isObject(%list)) {
		return;
	}

	for (%i = 0; %i < %list.getSize(); %i ++) {
		%mission = %list.getEntry(%i);
		//Send them the file

		%info = getMissionInfo(%mission.file, true);
		%collection.addEntry(%info);
	}
}

function getRandomHuntMap() {
	%ml = getMissionList("mp");

	%list = Array(CycleMapListArray);
	%difficulties = %ml.getDifficultyList("Hunt");
	%dcount = getRecordCount(%difficulties);
	for (%i = 0; %i < %dcount; %i ++) {
		%difficulty = getRecord(%difficulties, %i);
		%difficultyName = getField(%difficulty, 0);
		collectMissions(%ml, "Hunt", %difficultyName, %list);
	}
	collectMissions(%ml, "CustomHunt", "Custom", %list);

	//Play a random one
	%mis = %list.getEntry(getRandom(%list.getSize() - 1));

	%list.delete();

	return %mis;
}

