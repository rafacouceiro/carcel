# **Agentic Prison Guards System In Unity**

## **Description**
This project consists of a set of agents in a prison environment, which have to avoid the prisoners breaking out of the prison. The project is made using Unity and C#.

## **World State**
The **World State** is a data structure representing the agent's current "mental map" and internal conditions. It is used by the HTN planner to evaluate preconditions and effects.

### **1. Perceptions**
* **Fugitive-in-Vision**: A boolean indicating if the fugitive is currently within the agent's visual field.
* **Sound/Noise**: Detected noises (such as running or jumping) that increase the agent's alertness. Agent should have an approximate mental map of where the sound is comming from and somehow its timestamp, maybe just ignoring oldest sounds. Agent must not be able to distinguish between sounds from fugitive / other agents.
* **Other agents visual map**: Agent must have a mental map of the other agents' positions and its timestamps.
* **LKP (Last Known Position)**: The last recorded coordinates of the fugitive based solely on vision.
* **Prisoner-in-Cell**: A logical state confirming if the prisoner is contained; if false, the agent prioritizes search tasks. It always starts as True.
* **Current Location**: The specific node or sensitive position where the agent is currently located.
* **Past Locations**: A list of the agent's past locations, used to avoid revisiting the same location.

### **2. Internal States & Emotions**
* **Alertness**: A level that rises due to external stimuli like noise or suspicious world changes.
* **Fatigue (Tiredness)**: A physical attribute that limits movement speed and available physical actions.

**WARNING**: Agents have strictly **FORBIDDEN** communication between each other, this is not a multiagent system, agents are not allowed to share information between each other.

---

## **HTN Logic & Task Hierarchy**

The system uses a **Hierarchical Task Network (HTN)** to decompose high-level goals into executable primitive operators. This will be the **design guideline** to implement intelligence in our agents.

### **I. Emergency Task (Maximum Priority)**
Triggered when the intruder is seen or has been seen very recently.
* **Goal**: Catch the objective as efficiently as possible.
* **Preconditions**: `Fugitive-in-Vision` is true and we have enough energy to chase the fugitive.
* **Method (Pursue Intruder)**:
    1. **Select Destination**: Targets the fugitive's current or last seen location.
    2. **Run To**: High-speed movement toward the target, limited by fatigue.
    3. **Catch**: The final interaction to secure the fugitive.
* **Effects**: Max alertness is triggered, and `Prisoner-in-Cell` is set to true upon success.

### **II. Alert Task (Medium Priority)**
Triggered by excessive noise or the realization that a cell is empty.
* **Methods**:
    * **Investigate Noise**: Move to the timestamped origin of a detected sound.
        * **Preconditions**: an event like a strong sound has risen out alertness level and we have enough energy to investigate.
        * **Primitive Tasks**: **Choose investigation destination** and **Move To node**, **visually inspect the area**. The search will be based mainly on sound stimulus and maybe on sensitive positions.
        * **Effects**: depends on the outcome, we can find the suspect or not, also alertness will be reduced after a time investigating (not immediately).

    * **Investigate Fugue**: Search likely escape routes based on the LKP.
        * **Preconditions**: the agent is aware that figitive is not in his cell, this would happen if he sees the prisoner's cell empty.
        * **Primitive Tasks**: **Choose investigation destination** and M**ove To node**, **visually inspect the area**. The search will be based mainly on  on sensitive positions, as it is the most likely place to find the fugitive.
        * **Effects**: depends on the outcome, we can find the suspect or not, also alertness will be reduced after a time investigating (not immediately).


### **III. Routine Task (Low Priority)**
Standard behavior when no immediate threats are detected.

* **Methods**:
    * **Patrol/Round**: Follow a predefined or randomized path through map nodes:
        * **Preconditions**: None, maybe a minimum amount of energy.
        * **Primitive Tasks**: **choose a set of checkoints and move to them**. Agent will avoid revisiting recent locations and will try to avoid redundancy with other agents. Some randomness can be added to the path to avoid predictability and increase exploration.
        * **Effects**:.
    * **Cell Inspection**: Explicitly check cells to update visual information.
        * **Preconditions**: Amount of energy.
        * **Primitive Tasks**: **choose a set of cells to visit** and visually inspect them. Could be for example, the closest set of cells to the agent, or a set of cells that are not visited recently.
        * **Effects**: update visual information.
    * **Guard**: Stay at a sensitive position to watch and recover energy.
        * **Preconditions**: Certain level of tiredness.
        * **Primitive Tasks**: choose a stpot to guard based on sensitivty and distance from current location and other agents, could be basend on a utility function.
        * **Effects**: move there.

---

## **Primitive Operators**
The final actions that interact with Unity's NavMesh and Animator.
* **Move To (Destination, Speed)**: Navigates to a node and consumes energy.
* **Catch**: Changes the world state to `Prisoner-in-Cell = true`.
* **Inspect (Look)**: Agent visually inspects an area, important to gather visual information about the environment.
* **Choose destination**: the method for choosing the destiantion will depend on the method being used, is not the same choosing the fugitive, investigating noise, or guarding a sensitive position. This is a really importatn part of the project to disscuss.

---

## **Replanification**

As it is really important in HTN, we need a good replanification system. As stabished in HTN, it will replan if:

- The current plan has failes/succeded.
- There is a change in the world state: eg. a suspect noise is heard.
- There is a change in the agent's state: eg. the agent is too tired.

Our system will need to be 'alive' 24/7 getting updates on iself and its enviroment, replanning as quick as possible.

Game finishes when fugitive is cought.