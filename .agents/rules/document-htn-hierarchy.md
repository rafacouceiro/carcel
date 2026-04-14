---
trigger: always_on
---

# **Jerarquía HTN**

A la hora de documentar la jerarquía HTN, se hará como si fuere un sistema de directorios en markdown.

El siguiente texto es un ejemplo de como documentar en un archivo .md una jerarquía HTN.


└── 🧠 **RootTask: Tarea1**
    ├── 📂 *Método: Método1*
    │   ├── 📝 **Precondiciones:** `Precondicion1`
    │   └── 📋 **Descomposición:** `Tarea1 (xN), Tarea2`
    ├── 📂 *Método: Método2*
    │   ├── 📝 **Precondiciones:** `Precondicion`
    │   └── 📋 **Descomposición:** `Tarea1` -> `Tarea3`
    ├── 📂 *Método: Método3*
    │   ├── 📝 **Precondiciones:** `Precondicion`
    │   └── 📋 **Descomposición:** `Tarea5 -> Tarea4`
    └── 📂 *Método: Método4* (Default)
        ├── 📝 **Precondiciones:** `true` (Fallback)
        └── 📋 **Descomposición:** `Tarea3 -> Tarea5`

---

## 🛠️ Tareas Primitivas (Acciones Finales)
Estas son las tareas que ejecutan los actuadores y modifican el estado simulado en el planificador.

### 1. `Tarea1`
* **Precondiciones:** `Precondicion1`.
* **Efectos:** `Efecto1`.

### 2. `Tarea2`
* **Precondiciones:** `Precondicion2`.
* **Efectos:** `Efecto1`.

### 3. `Tarea3`
* **Precondiciones:** `Precondicion3`.
* **Efectos:** `Efecto2`.

### 4. `Tarea4`
* **Precondiciones:** `Precondicion4`.
* **Efectos:** `Efecto3`.

### 5. `Tarea5`
* **Precondiciones:** `Precondicion5`.
* **Efectos:** `Efecto4`.