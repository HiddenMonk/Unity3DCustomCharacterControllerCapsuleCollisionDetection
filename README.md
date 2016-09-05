##Unity3DCustomCharacterControllerCapsuleCollisionDetection
A custom unity3d capsule character controller that uses its own collision detection system.

"Great repository names are short and memorable." - github


[![DemoVideo](http://img.youtube.com/vi/hlQCDaWBfGQ/0.jpg)](https://www.youtube.com/watch?v=hlQCDaWBfGQ)

----------------------------------------------------------------------------------------------

I have spent a depressing amount of time trying to get a custom character controller due to some of unitys character controller limitations (such as only rotation around the Y) as well as just the desire to have more control over how things work. I kept trying to make a perfect character controller, but failed again and again and while this may still not be perfect, it is just going to have to do for me. Maybe it would be of help to you as other open source code were to me in creating this.

Although, if you are looking for something better in terms of custom physics, maybe look into using whole physics systems like this free asset of bullet physics being integrated with unity.

https://www.assetstore.unity3d.com/en/#!/content/62991

Or this one for jitter physics

http://forum.unity3d.com/threads/jitter-physics-engine-vs-built-in-physx.186325/


All in all, you should mainly be using this custom character controller if you want to attempt to make a better one with some of the collision code here or if you are desperate and really need... just something! I didnt really know of these open source C# physics system like bullet, jitter, bepu, etc..., but even now that I do I dont really want to integrate these large scary projects that I have no idea how it works.


###To get it out of the way, here are some downsides.

###--Unity Version--

Requires Physics.OverlapCapsule which was introduced in unity 5.4. You might be able to get away with multiple OverlapSpheres if you really needed to.

###--Performance--

Performance compared to physX is pretty bad. Primitive colliders are fine, its the mesh colliders that have issues... kinda. It can be bad depending on how many triangles you are colliding with. For example, I have a 15,000 triangle mesh that I can get just fine performance so long as that mesh is scaled large enough that I am only colliding with 5 or so triangles or whatever. However, if it was a small object and I was touching half of those triangles, I would need to do calculations on all those triangles. This, at least in my case, should not be an issue.

###--Uniform Scale--

Many times I assume uniform scale. The primitive colliders might be able to be scaled non uniformly, but the mesh colliders will start to get weird results. The scale can be (1,1,1), (11.45, 11.45, 11.45), etc... just not something like (1, 456, 24).

###--No terrain collider--

I dont use it.

###--Character Controller--

When I say character controller, I mainly mean the collision handling and what not, similar to the unity character controller component. I dont mean a character controller such as one that handles walking animations and such. The "CharacterController" is called PlayerRigidbody in this project.

The character controller has limitations and is made in a way that is fine for my game, but might not be for yours.
It uses an iterative approach for depenetation since I kept failing to utilize a capsulecast as it was too sensitive to errors.
It might never be possible to do a proper step offseting (I think you would need to do the capsule casts way...).
It doesnt handle sliding off slopes properly, you will not slide upwards in he air, but will just go straight.
And probably more =), however, you can of course attempt to change the way the PlayerRigidbody works.


###Some good things?

###--Framerate independence--

The character controller is designed in a way to acheive good framerate independence.

###--Garbage collection--

Should be pretty garbage free. No Foreach used and what not...

###--Open Source--

Its open source, so dig in and learn =)

--------------------------------------------------------------------------------------------------------

I started off trying to utilize capsule casts, then I ended up finding a open source character controller that had its own collision detection, however, it could only handle spheres.

Here is that project - https://github.com/IronWarrior/SuperCharacterController

They would use multiple spheres to create a capsule shape. I took that and started to get some good results, but kept running into issues. I have found that using multiple spheres is not good enough due to reasons such as running and then jumping against an edge. There would be a sphere ontop that would detect the normal one way, while a sphere lower down would detect the normal a different way. Maybe I could have found the average and what not, but I quickly decided to see if I could just implement an actual capsule collision detection. Thanks to the internet, I was able to do so fairly quickly.

So what is the logic behind the capsule collision detection?
From reading online I have found that you just treat the capsule as a line segment. So all the collision methods are pretty much broken down into LineSegment-XXXX methods like ClosestPointOnLineSegmentToPoint for Capsule-Sphere, ClosestPointsOnTwoLineSegments for Capsule-Capsule, and ClosestPointOnRectangleToLine for Capsule-Box and Capsule-Mesh (triangles). The line segment methods are usually just line methods and then we clamp the result onto the line segment.

The source code should be pretty clear, but here are some images to help explain.

![Capsule-Sphere](https://github.com/HiddenMonk/Unity3DCustomCharacterControllerCapsuleCollisionDetection/blob/master/Images/Capsule-Sphere.png)
![Capsule-Capsule](https://github.com/HiddenMonk/Unity3DCustomCharacterControllerCapsuleCollisionDetection/blob/master/Images/Capsule-Capsule.png)
![Capsule-Box](https://github.com/HiddenMonk/Unity3DCustomCharacterControllerCapsuleCollisionDetection/blob/master/Images/Capsule-Box.png)


For the mesh colliders, we are using a AABB (axis aligned bounding box) tree to help speed up finding the desired triangles. I was originally using the BSPTree in the Super character controller project, however I think the change to an AABB tree (I think its called that) helped speed things up. The BSPTree had issues with duplicating triangles. I had a 15k triangle mesh that the BSPTree was splitting it into I think 150k triangle tree. The AABB tree has no duplicates.

Here is a video demonstrating the Capsule-Mesh collision detection.

[![MeshAABBDemo](http://img.youtube.com/vi/Wxatc2AAvno/0.jpg)](https://www.youtube.com/watch?v=Wxatc2AAvno)


The way its set up is similar to how the BSPTree was setup. We take all the triangles and decide whether to split them to the positive side or the negative side, and the repeat until each box contains only a few triangles. So in the end we will have 1 giant box that contains 2 boxes and those 2 boxes will contain 2 boxes within them which will hold 2 boxes within them, etc... Now to find the triangles we are touching, we first touch the big box, then we check to see which of the 2 boxes inside the big box are are touching and then go inside that box and check the 2 boxes in there and keep doing that until we cant go no more.

Then we take all the possible closest triangles and do something similar to what we did for our Capsule-Box collision which is to find the closest point between the capsule and the triangle. The ClosestPointOnTriangleToLine and ClosestPointOnRectangleToLine are literally the same method except the rectangle needs to check 1 more edge.

After we get our closest points, if we are using the multipleContacts version of the ClosestPointsOnSurface, I do a "CleanUp" so that I am not just given all these useless contact points. The cleanup method can be very very slow depending on how many contacts there are since we first sort them from closest to farthest, and then we check if a point is behind another points normal plane and if so then we assume its useless to us since the first point should be enough. The cleanup method is not perfect, is very bad for performance, but its all I got for now =).

I saw in the Super character controller project there was a deprecated RPGMesh tree that was instead of making a tree for each mesh, it creates the tree for a mesh once and stores it so any same mesh can just use it. I liked it so I implemented it as well. The next step I guess would be to save it to file so you dont have to make the trees at runtime every time, but that might not be needed depending on your mesh poly count.


###--The Character Controller--

I cant use unitys character controller because my game needs the character to rotate on all axis, which unitys character controller cannot do as far as I know, and I dont want to use a rigidbody because I dont want to deal with FixedUpdate as well as fighting with the physics system. If I also want to utilize OnCollisionStay and what not, currently they cause a ton of garbage. The rigidbodies also have a "feature" for performance to where even if you have Continuous Collision Detection set, at slow speeds it will still penetrate into objects which looks terrible when you have a camera follow it. You can have the physics run more times by changing the timestep (and honestly it would still probably be way more performant friendly than this custom character controller), but that still leads me with fighting the physics (which everything will be 1 frame delayed due to how the physics works..) and dealing with the problems FixedUpdate has such as stutter for cameras (possibly ways to avoid).


A big issue I had with making my own character controller was making it framerate independent. This is something that is relevant regardless if you are using unitys character controller or not so long as it is running in Update (variable timestep) and not FixedUpdate (fixed timestep). I made a thread about my troubles on this and you can see the different methods I was reading about here http://forum.unity3d.com/threads/movement-consistency-and-timesteps-framerate.365703/ 
(notice the thread was made almost a year ago lol...).

What I ended up going with was something like a FixedUpdate in which we are taking into account how long this frame is and running our character rigidbody multiple times as needed. Later on somehow I found out that Unreal Engine actually doesnt have a FixedUpdate like unity, but instead has basically what I was using which is a substepper.

There is a nice post about it here http://www.aclockworkberry.com/unreal-engine-substepping/

I ended up changing the way I was handling the variables to match how unreal was handling it. Something I still am not too sure on how to do properly is timed movements, which you can see what I had to say about that here
http://forum.unity3d.com/threads/movement-consistency-problems-distance-and-speed-within-a-time.409148/


###--So how does the PlayerRigidbody basically work?--

In our GetCollisionSafeVelocity we first divide our velocity up so that each time we move we dont move more than a certain safe amount. Ideally you would want to use a capsulecast or something, but I kept running into issues so I am just doing it this way which is less accurate and blah blah, hopefully its at least consistent and good enough.

The first big thing we do, which I had lots of issues with, is the grounding. If we were to do a capsulecast and what not, we might be able to avoid this whole mess, however, since we are relying on penetrating into objects and depenetrating, we will have to do a buncha hacks for our grounding in order to make sure it stays consistent whether on slopes, edges, etc...
There is a lot of notes in the code if you would like to study that giant mess of a hack. I honestly dont want to look at it =).

I will mention 2 methods that I use in my grounding which is the DepenetrateSphereFromPlaneInDirection and SpherePositionBetween2Planes. I explain the DepenetrateSphereFromPlaneInDirection in a thread here
http://forum.unity3d.com/threads/need-algorithm-for-depenetrating-sphere-from-wall-in-a-direction.369526/
As for the SpherePositionBetween2Planes, we are trying to find the minimum distance where a sphere would fit between 2 planes.

Here is an image showing what I mean

![SpherePositionBetween2Planes](https://github.com/HiddenMonk/Unity3DCustomCharacterControllerCapsuleCollisionDetection/blob/master/Images/SpherePositionBetween2Planes.png)

Unfortunately I cant just rely on those 2 methods since they assume an infinite plane and that just isnt the case.

Once we do our grounding, we gather any contact points we might have and then do 2 optional things - TryBlockAtSlopeLimit and CleanByIgnoreBehindPlane. TryBlockAtSlopeLimit tries to stop us from going up slopes that are higher than our slope limit as if there is a wall there, and CleanByIgnoreBehindPlane tries to remove any contact points that we might not be interested in. For example, imagine we penetrate into the ground, but under the ground there was a box for whatever reason that we collided into the corner of. That might cause our depenetration method to depenetrate weirdly since it detected something there, but what CleanByIgnoreBehindPlane would do is detect that the box contact point is below the ground contant point and remove it. I have not really tested it so I dont know how well it works or its performance (if you have a lot of contact points then the performance would probably be pretty bad).

Now we get to the next big issue I had with making a character controller which was handling the collision/depenetration. I tried different things and many times I would get very close, but not good enough, especially when it came to being framerate independent. The way I depenetrated or handled grounding affected my chances at framerate independence. 1, 2, skip a few, my depenetration method is nothing special (I think its similar to the one in the super character controller). It handles everything as if it is a sphere (you can basically imagine a capsule with an infinite amount of spheres in it). I used this method when I was using multiple spheres as my collider, but it works just as well for capsules as long as you set up the collision infos correctly.
Here is an explanation the guy behind the super character controller gave on the subject https://roystanross.wordpress.com/2014/05/07/custom-character-controller-in-unity-part-1-collision-resolution/

The depenetration method allows you to decide how many times you want it to iterate with the old collision info to depenetrate itself, while my PlayerRigidbody allows you to choose how many iterations you will detect new collision info to keep things up to date. You will basically need to find a balance you are happy with the 2.

Here is a video demonstrating the depenetration method.

[![DepenetrationDemo](http://img.youtube.com/vi/piLQ649XGLM/0.jpg)](https://www.youtube.com/watch?v=piLQ649XGLM)


When we increase the Detection Iterations you see the capsule depenetration properly, but there are a lot of blue rays being drawn. Those blue rays are the new contact points that we are detecting each detection iteration. When we lower the detection iterations and increase the Depenetration Iterations, you notice less blue rays as well as our depenetration getting weird towards the end of the box. This is because we are using old collision data to try and save on performance, but that leads to less accuracy. If the boxes were infinite, we probably wouldnt notice any issues.

After we depenetrated, its time to redirect our velocity. In order to be able to stand on slopes, I flatten my velocity. The bad thing about this is that if I am going up a slope and fly off, I will just shoot straight and not up in the air naturally. I am just going to live with that since I dont think my game cares for it.

That is kinda the attitude I have in regards to my character controller. I am just going to live with it. It might have bugs, limitations, not be the best performance wise, etc..., but I just want to start focusing on the game instead of the controller T.T

If you are looking for something better then... welll, the super character controller was my starting point and now I am hopefully satisfied, so perhaps this can now be your starting point =).

###--Some Sources--

https://github.com/IronWarrior/SuperCharacterController

http://gamedev.stackexchange.com/questions/5585/line-triangle-intersection-last-bits

https://studiofreya.com/3d-math-and-physics/simple-aabb-vs-aabb-collision-detection/

http://wiki.unity3d.com/index.php/3d_Math_functions

I dont think I used anything from geometrictools, but I did keep going back to it to read the code and try to understand things. Many times it looked too complicated (possibly to be optimized and handle many edge cases). I will put it here anyways since it has lots of nice open source code.

https://www.geometrictools.com/

I just want to put this free asset I found here. Its an asset that has a bunch of handy Debug.Draws such as Capsule, Sphere, etc...

https://www.assetstore.unity3d.com/en/#!/content/11396
