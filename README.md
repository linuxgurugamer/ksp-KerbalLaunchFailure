# Kerbal Launch Failure
Kerbal Launch Failure is a lightweight, random launch failure mod for Kerbal Space Program. You might want to start using that Launch Escape System with this installed.

### About
NASA requires that human-rated spacecraft have a probability of loss on ascent of no more than 1 in 500. The Kerbals of planet Kerbin think of this requirement as silly, so they have instead adopted a less strict policy of a loss on ascent of no more than 1 in 50 for their Kerbal Space Program. In fact, to save funds on safety measures, they have made their spacecraft to fail exactly 1 in 50 times.

This mod is not meant to compete with other part failure mods (I personally enjoy using DangIt! with Entropy), but to be an alternative for those that want that living on the edge feel for launches without having to worry about part failures and repairs beyond the initial launch.

### Current features
* Launch has a small chance (1 in 50) of failure with explosions during ascent.
* Failure originates in an engine, which will begin to overheat and have extra thrust applied to cause instability.
* The abort action group can be set to auto-trigger once the initial failure occurs (custom setting; see below).
* Explosion has a chance to propagate to surrounding parts, varying how catastrophic the failure is.
* Tanks containing fuel are more likely to explode.
* Failure checks end once the spacecraft has reach space (IRL, disasters mostly happen during ascent anyways). This is to prevent frustration for longer missions, to reduce the complexity of the mod, and to lessen the CPU usage for game ticks.

### Custom settings
Settings file can be found at "GameData/KerbalLaunchFailure/Plugins/PluginData/KLF_Settings.cfg"
* **initialFailureProbability** - Sets the probability that a failure will occur during launch ascent. Default is 2% (1 in 50 chance).
* **maxFailureAltitudePercentage** - The max height at which the failure will occur as a percentage of atmosphere depth. Default is 65% (for stock Kerbin, about 45.5km up); recommend not going above this amount for right now.
* **propagationChanceDecreases** - The chance of a failure to propagate to a nearby part. Set to True to greatly lessen the explosiveness of a failure. Default is False.
* **failurePropagateProbability** - The probability that a failure will propagate to nearby parts. Does not affect parts with fuel; their behavior is different. This gradually decreases if propagationChanceDecreases set to True.
* **delayBetweenPartFailures** - The attempted time delay in seconds between part failures (not sure if working as intended, will test further). Default is 0.2 seconds. Try increasing if game performance takes a hit during a failure.
* **autoAbort** - If set True and a failure occurs, will trigger the abort action group automatically. Default is False.

### Possible upcoming features
* More "descriptive" flight logs using optional and random technobabble.
* More organic method of propagating failures using the stock heating model when it becomes more finalized.
* Optional random failure of heat shield during re-entry, maybe.
* Maybe more varying types of failures specific to ascent (e.g. loss of yaw control).

### Will not implement
* Random part failures outside of launch/re-entry and repairs (check out [DangIt!](http://forum.kerbalspaceprogram.com/threads/81794) by Ippo / Coffeeman, or [Kerbal Mechanics](http://forum.kerbalspaceprogram.com/threads/85798) by Nifty255).

### License
This plugin is released under the GNU General Public License: http://www.gnu.org/licenses/.

### Changelog
5/29/2015 - **v1.0.0** *Albinoni Allegretto* - First production release. Added custom settings.

5/1/2015 - **v0.1.0** - Alpha release. The part that starts the failure overheats and overthrusts to allow people time to react. Auto abort action group trigger added.

4/28/2015 - **v0.0.1** - Alpha test release for KSP 1.0.
