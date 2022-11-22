from mesa import Agent, Model 
from mesa.space import MultiGrid
from mesa.time import SimultaneousActivation
from mesa.datacollection import DataCollector
import numpy as np


#Clase que se encarga de crear las cajas.
class Cajas(Agent):
    def __init__(self, unique_id, model):
        super().__init__(unique_id, model)
        self.agent = "gridcaja"

#Clase para apilar las cajas.
class Almacenar(Agent):
    def __init__(self, unique_id, model):
        super().__init__(unique_id, model)
        self.agent = "apilar"
        self.cantidad = 0

#Clase paredes.
class Paredes(Agent):
    def __init__(self, unique_id, model):
        super().__init__(unique_id, model)
        self.agent = "pared"

#Creación de robots.
class Robots(Agent):
    def __init__(self, unique_id, model):
        super().__init__(unique_id, model)
        self.agent = "robot"
        self.caja = False
        self.movimientos = 0
        self.cajassinalmacenar = self.model.cajas
    #Unión de unity y el modelo por medio de prefabs.
    def Unir(self):
        celdasvacias = self.model.grid.get_cell_list_contents([self.pos])
        if len(celdasvacias) != 0:
            for i in celdasvacias:
                if i.agent == "gridcaja" and self.agent == "robot":
                    self.caja = True
                    self.agent = "carga"
                    i.agent = "sinagente"
                elif i.agent == "apilar":
                    if self.agent == "carga":
                        self.agent = "robot"
                        self.caja = False
                elif i.agent == "estantelleno" and self.caja == True:
                    self.caja = True
                    self.agent = "carga"
                    i.agent = "estantelleno"


#Movimiento de los robots
    def Mover(self):
        siguienteposicion = self.model.grid.get_neighborhood(
            self.pos,
            moore=False,
            include_center=False,
            radius=1)
        nuevaposicion = self.random.choice(siguienteposicion)
        cambiarceldas = self.model.grid.get_cell_list_contents([nuevaposicion])
        if len(cambiarceldas) == 1:
            if cambiarceldas[0].agent != "robot" and \
                cambiarceldas[0].agent != "carga" and \
                cambiarceldas[0].agent != "pared" and \
                cambiarceldas[0].agent != "apilar" and \
                cambiarceldas[0].agent != "estantelleno":
                self.model.grid.move_agent(self, nuevaposicion)
                self.movimientos += 1
        elif len(cambiarceldas) == 0:
            self.model.grid.move_agent(self, nuevaposicion)
            self.movimientos += 1


 #Cuando el robot encuentra una caja, la lleva a los estantes.
    def Estantes(self):
        posicionX = self.pos[0] - self.model.posicionesPilas[0][0]
        posicionY = self.pos[1] - self.model.posicionesPilas[0][1]
        if posicionX > 0:
            nuevaposicion = (self.pos[0] - 1, self.pos[1])
            cambiarceldas = self.model.grid.get_cell_list_contents([nuevaposicion])
            if len(cambiarceldas) == 1:
                if cambiarceldas[0].agent != "robot" and \
                cambiarceldas[0].agent != "carga" and \
                cambiarceldas[0].agent != "pared":
                    self.model.grid.move_agent(self, nuevaposicion)
                    self.movimientos += 1
            elif len(cambiarceldas) == 0:
                self.model.grid.move_agent(self, nuevaposicion)
                self.movimientos += 1
        elif posicionX < 0:
            nuevaposicion = (self.pos[0] + 1, self.pos[1])
            cambiarceldas = self.model.grid.get_cell_list_contents([nuevaposicion])
            if len(cambiarceldas) == 1:
                if cambiarceldas[0].agent != "robot" and \
                cambiarceldas[0].agent != "carga" and \
                cambiarceldas[0].agent != "pared" :
                    self.model.grid.move_agent(self, nuevaposicion)
                    self.movimientos += 1
            elif len(cambiarceldas) == 0:
                self.model.grid.move_agent(self, nuevaposicion)
                self.movimientos += 1
        elif posicionY > 0:
            nuevaposicion = (self.pos[0], self.pos[1] - 1)
            cambiarceldas = self.model.grid.get_cell_list_contents([nuevaposicion])
            if len(cambiarceldas) == 1:
                if cambiarceldas[0].agent != "robot" and \
                cambiarceldas[0].agent != "carga" and \
                cambiarceldas[0].agent != "pared":
                    self.model.grid.move_agent(self, nuevaposicion)
                    self.movimientos += 1
            elif len(cambiarceldas) == 0:
                self.model.grid.move_agent(self, nuevaposicion)
                self.movimientos += 1
        elif posicionY < 0:
            nuevaposicion = (self.pos[0], self.pos[1] + 1)
            cambiarceldas = self.model.grid.get_cell_list_contents([nuevaposicion])
            if len(cambiarceldas) == 1:
                if cambiarceldas[0].agent != "robot" and \
                cambiarceldas[0].agent != "carga" and \
                cambiarceldas[0].agent != "pared":
                    self.model.grid.move_agent(self, nuevaposicion)
                    self.movimientos += 1
            elif len(cambiarceldas) == 0:
                self.model.grid.move_agent(self, nuevaposicion)
                self.movimientos += 1

#Realizar x cantidad de pasos
    def step(self):
        self.cajassinalmacenar = self.model.cajas
        self.Unir()
        if self.model.pasosTotales > 0 and self.model.cajas > 0:
            if self.caja == True:
                self.Estantes()
                self.model.pasosTotales -= 1
            else:
                self.Mover()
                self.model.pasosTotales -= 1
            celdasvacias = self.model.grid.get_cell_list_contents([self.pos])
            for i in celdasvacias:
                if i.agent == "estantelleno":
                    if self.caja == True:
                        self.Estantes()
                        self.model.pasosTotales -= 1
                elif i.agent == "apilar":
                    if self.caja == True:
                        if i.cantidad == 4:
                            i.cantidad = 5
                            i.agent = "estantelleno"
                            estantelleno = [i.pos[0], i.pos[1]]
                            self.model.posicionesPilas.remove(estantelleno)
                            self.agent = "robot"
                            self.caja = False
                            self.model.cajas -= 1
                        elif i.cantidad < 4:
                            i.cantidad += 1
                            self.agent = "robot"
                            self.caja = False
                            self.model.cajas -= 1
                    

class Modelo(Model):
#Modelo general
    def __init__(self, ancho, altura, robots, cajas, pasos):
        self.ancho = ancho
        self.alto = altura
        self.robots = robots
        self.cajas = cajas
        self.pasosTotales = pasos * robots
        self.grid = MultiGrid(ancho, altura, True)
        self.schedule = SimultaneousActivation(self)
        self.running = True
        posiciones = []
        self.almacen = (cajas // 5) + 1
        self.posicionesPilas = []
        self.movimientosfinales = DataCollector(
            model_reporters={"Movimientos finales":Modelo.movimientostotales},
            agent_reporters={}
        )
        self.cantidaddecajas = DataCollector(
            model_reporters={"Cajas sin almacenar":Modelo.cantidadcajas},
            agent_reporters={}
        )

        #Obtiene las posiciones ocupadas para saber que celdas están vacías.
        for (content, x, y) in self.grid.coord_iter():
            posiciones.append([x, y])

        #Paredes
        for i in range(0, altura):
            p = Paredes(i, self)
            self.grid.place_agent(p, (0, i))
            self.grid.place_agent(p, (ancho - 1, i))
            posicionpared = [0, i]
            posicionpared2 = [ancho - 1, i]
            posiciones.remove(posicionpared)
            posiciones.remove(posicionpared2)
        for i in range(1, ancho - 1):
            p = Paredes(i, self)
            self.grid.place_agent(p, (i, altura - 1))
            posicionpared = [i, altura - 1]
            posiciones.remove(posicionpared)
        for i in range(1, (ancho // 2)):
            p = Paredes(i, self)
            self.grid.place_agent(p, (i, 0))
            posicionpared = [i, 0]
            posiciones.remove(posicionpared)
        for i in range((ancho // 2) + 1, ancho - 1):
            p = Paredes(i, self)
            self.grid.place_agent(p, (i, 0))

        #Creacion de almacenes
        for i in range(self.almacen):
            a = Almacenar(i, self)
            posicionesalmacenes = self.random.choice(posiciones)
            self.grid.place_agent(a, (posicionesalmacenes[0], posicionesalmacenes[1]))
            self.posicionesPilas.append(posicionesalmacenes)
            posiciones.remove(posicionesalmacenes)

        # Creacion de robots.
        for i in range(self.robots):
            r = Robots(i, self)
            self.schedule.add(r)
            posicionesrobots = self.random.choice(posiciones)
            self.grid.place_agent(r, (posicionesrobots[0], posicionesrobots[1]))
            posiciones.remove(posicionesrobots)
        
        # Creación de cajas.
        for i in range(self.cajas):
            c = Cajas(i, self)
            posicion = self.random.choice(posiciones)
            self.grid.place_agent(c, (posicion[0], posicion[1])) 
            posiciones.remove(posicion)

    def cantidadcajas(model):
        #Cantidad de cajas que quedan por almacenar
        cajastotales = [
            agent.cajassinalmacenar for agent in model.schedule.agents]
        for i in cajastotales:
            return i

    def step(self):
        #Cantidades necesarias
        self.schedule.step()
        self.movimientosfinales.collect(self)
        self.cantidaddecajas.collect(self)
        print("Cantidad de cajas: ", self.cajas)        
    
    def movimientostotales(model):
        #obtiene los movimientos totales
        movimientostotales = 0
        movimientos = [
            agent.movimientos for agent in model.schedule.agents]
        for i in movimientos:
            movimientostotales += i
        print("Movimientos totales: ",movimientostotales)
        return movimientostotales
