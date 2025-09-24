# Sole Survivor
<img width="1024" height="1024" alt="image" src="https://github.com/user-attachments/assets/d03bbac1-182d-4bbc-8e3e-e57baf2da70c" />

## Team Roles

---

### Maddox Barron  
- **Agile Board Director**  
- **Backend Game Logic**  
- **Playtester**

---

### Landon Prince  
- **GitHub Manager**  
- **Backend Game Logic**  
- **Playtester**

---

### Nick Bui  
- **Networking Lead**  
- **Backend Game Logic**  
- **Playtester**

---

### Matthew McCullough  
- **Audio/Visual Designer**  
- **Backend Game Logic**  
- **Playtester**



Project Notes and Findings:
Colliders: Adding a mesh collider to a prefab won't directly work if the prefab has sub components. Either use a box or add mesh to each sub component
Building: When building with multiple scenes, go to File->Build Profile->Scenes. Here you can move the scene you want to load into to the top of the list
Grabbable Items: 
  1. On Mesh Turn Off isTrigger (prevents falling through floor), Turn on Gravity, Turn of Kinematics, Change Collision Detection From Discrete To Continuous
  2. To prevent objects from "pushing back" and sending the player flying, we have created 3 layers. A player, scene, and grabbable object collision layer. Under Project Settings -> Physics -> Collision Matrix, uncheck the interaction between grabbables and player
