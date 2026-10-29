import math
import pathlib

import os

ROOT = pathlib.Path(os.environ.get("LEVELS_OUT", pathlib.Path(__file__).resolve().parents[2] / "scenes" / "levels"))

RESOURCES = [
    ("Script", "res://scripts/Level/RaceController.cs", "race"),
    ("Script", "res://scripts/Level/WarehouseBackground.cs", "background"),
    ("Script", "res://scripts/Elements/Crate.cs", "crate"),
    ("Script", "res://scripts/Elements/ConcreteBlock.cs", "concrete"),
    ("Script", "res://scripts/Elements/TimeClock.cs", "clock"),
    ("Script", "res://scripts/Elements/ExpressPackage.cs", "package"),
    ("Script", "res://scripts/Elements/LoadingDock.cs", "dock"),
    ("Script", "res://scripts/Elements/Checkpoint.cs", "checkpoint"),
    ("Script", "res://scripts/Elements/KillZone.cs", "killzone"),
    ("Script", "res://scripts/Elements/HintSign.cs", "sign"),
    ("Script", "res://scripts/Elements/ConveyorBelt.cs", "belt"),
    ("Script", "res://scripts/Elements/MovingPlatform.cs", "moving"),
    ("Script", "res://scripts/Elements/WarehouseRobot.cs", "robot"),
    ("Script", "res://scripts/Elements/ScanBeam.cs", "scan"),
    ("Script", "res://scripts/Elements/BubbleWrap.cs", "bubble"),
    ("Script", "res://scripts/Elements/LaunchRamp.cs", "launch"),
    ("Script", "res://scripts/Level/RolloutCoach.cs", "coach"),
    ("Script", "res://scripts/Level/RunRecorder.cs", "recorder"),
    ("Script", "res://scripts/Level/WallKickCoach.cs", "kickcoach"),
    ("PackedScene", "res://scenes/player/Player.tscn", "player"),
]


OPTIONAL_RESOURCES = {"coach", "recorder", "kickcoach"}

def num(v):
    return f"{v:g}"


def vec(x, y):
    return f"Vector2({num(x)}, {num(y)})"


class Level:
    def __init__(self, scene_name, level_id, left, right, top, bottom=0):
        self.scene_name = scene_name
        self.level_id = level_id
        self.left = left
        self.right = right
        self.top = top
        self.bottom = bottom
        self.nodes = []
        self.counts = {}

    def _name(self, base):
        self.counts[base] = self.counts.get(base, 0) + 1
        return f"{base}{self.counts[base]}"

    def _add(self, base, type_, script, props, parent="."):
        lines = [f'[node name="{self._name(base)}" type="{type_}" parent="{parent}"]']
        lines += props
        lines.append(f'script = ExtResource("{script}")')
        self.nodes.append((parent, lines))

    def block(self, kind, x0, x1, top, bottom=0):
        base = "Crate" if kind == "crate" else "Concrete"
        self._add(base, "StaticBody2D", kind, [f"position = {vec((x0 + x1) / 2, (top + bottom) / 2)}"], "Course")
        self.nodes[-1][1].append(f"Size = {vec(x1 - x0, bottom - top)}")

    def floor(self, x0, x1, y=0):
        self.block("concrete", x0, x1, y, y + 200)

    def slide(self, x0, y0, length, degrees, thickness=60):
        a = math.radians(degrees)
        x1, y1 = x0 + length * math.cos(a), y0 + length * math.sin(a)
        cx = (x0 + x1) / 2 - math.sin(a) * thickness / 2
        cy = (y0 + y1) / 2 + math.cos(a) * thickness / 2
        self._add("Crate", "StaticBody2D", "crate", [f"position = {vec(round(cx, 2), round(cy, 2))}", f"rotation = {a:.6f}"], "Course")
        self.nodes[-1][1].append(f"Size = {vec(length, thickness)}")
        return (round(x1), round(y1))

    def slide_left(self, x_top, y_top, length, degrees, thickness=60):
        a = math.radians(degrees)
        x_low, y_low = x_top - length * math.cos(a), y_top + length * math.sin(a)
        self.slide(x_low, y_low, length, -degrees, thickness)
        return (round(x_low), round(y_low))

    def walls(self):
        self.block("concrete", self.left - 60, self.left, self.top - 200, self.bottom + 200)
        self.block("concrete", self.right, self.right + 60, self.top - 200, self.bottom + 200)

    def sign(self, x, text, y=0):
        self._add("Sign", "Node2D", "sign", [f"position = {vec(x, y)}"])
        self.nodes[-1][1].append('Text = "' + text.replace("\n", "\\n") + '"')

    def clock(self, x, clock_out=False, y=0):
        self._add("ClockOut" if clock_out else "ClockIn", "Area2D", "clock", [f"position = {vec(x, y)}"])
        if clock_out:
            self.nodes[-1][1].append("Mode = 1")

    def package(self, x, y):
        self._add("ExpressPackage", "Area2D", "package", [f"position = {vec(x, y)}"])

    def dock(self, x, y=0):
        self._add("LoadingDock", "Area2D", "dock", [f"position = {vec(x, y)}"])

    def checkpoint(self, x, y=0):
        self._add("Checkpoint", "Area2D", "checkpoint", [f"position = {vec(x, y)}"])

    def belt(self, p0, p1, speed, thickness=20):
        angle = math.atan2(p1[1] - p0[1], p1[0] - p0[0])
        length = math.hypot(p1[0] - p0[0], p1[1] - p0[1]) + 8
        mx, my = (p0[0] + p1[0]) / 2, (p0[1] + p1[1]) / 2
        cx, cy = mx - math.sin(angle) * thickness / 2, my + math.cos(angle) * thickness / 2
        self._add("Belt", "StaticBody2D", "belt", [f"position = {vec(round(cx, 2), round(cy, 2))}", f"rotation = {angle:.6f}"], "Course")
        self.nodes[-1][1].extend([f"Size = {vec(round(length, 1), thickness)}", f"BeltSpeed = {num(speed)}"])

    def speed_jump(self, x0, y0, speed):
        drop, flat, rise = (260, 150), 300, (262, 184)
        a = (x0, y0)
        b = (x0 + drop[0], y0 + drop[1])
        c = (b[0] + flat, b[1])
        d = (c[0] + rise[0], c[1] - rise[1])
        self.belt(a, b, speed)
        self.belt(b, c, speed)
        self.belt(c, d, speed)
        return d

    def moving(self, x, y, w, h, travel, period, phase=0.0, look=0, script="moving", base="Moving"):
        self._add(base, "AnimatableBody2D", script, [f"position = {vec(x, y)}"])
        self.nodes[-1][1].extend([f"Size = {vec(w, h)}", f"Travel = {vec(*travel)}", f"Period = {num(period)}", f"Phase = {num(phase)}"])
        if script == "moving":
            self.nodes[-1][1].append(f"Look = {look}")

    def robot(self, x, travel, period, phase=0.0):
        self.moving(x, -22, 90, 44, (travel, 0), period, phase, script="robot", base="Robot")

    def scanner(self, x, height, on, off, phase):
        self._add("Scanner", "Area2D", "scan", [f"position = {vec(x, 0)}"])
        self.nodes[-1][1].extend([f"TriggerSize = {vec(16, height)}", f"OnTime = {num(on)}", f"OffTime = {num(off)}", f"Phase = {num(phase)}"])

    def bubble_pad(self, x, top, width=120):
        self.block("concrete", x - 40, x + 40, top + 30, 400)
        self._add("BubbleWrap", "StaticBody2D", "bubble", [f"position = {vec(x, top + 15)}"])
        self.nodes[-1][1].append(f"Size = {vec(width, 30)}")

    def tilted_bubble(self, x_top, y_top, length, degrees):
        a = math.radians(degrees)
        cx = x_top + length / 2 * math.cos(a) - math.sin(a) * 15
        cy = y_top + length / 2 * math.sin(a) + math.cos(a) * 15
        self._add("BubbleWrap", "StaticBody2D", "bubble", [f"position = {vec(round(cx, 2), round(cy, 2))}", f"rotation = {a:.6f}"])
        self.nodes[-1][1].append(f"Size = {vec(length, 30)}")

    def launch_ramp(self, x, launch, y=0):
        self._add("LaunchRamp", "StaticBody2D", "launch", [f"position = {vec(x, y)}"])
        self.nodes[-1][1].append(f"LaunchVelocity = {vec(*launch)}")

    def add_launch_y(self, y):
        for parent, lines in reversed(self.nodes):
            if "LaunchRamp" in lines[0]:
                lines[1] = lines[1].replace(", 0)", f", {num(y)})")
                return

    def fleeing_package(self, x, y, path):
        self.package(x, y)
        flat = ", ".join(f"{num(px)}, {num(py)}" for px, py in path)
        self.nodes[-1][1].append(f"FleePath = PackedVector2Array({flat})")

    def chimney(self, x, floor_y, height, gap, exit_len=0, belt_speed=350, wall=80):
        right_x0 = x + wall + gap
        right_x1 = right_x0 + wall
        top = floor_y - height
        self.block("crate", x, x + wall, top - 60, floor_y - 140)
        self.block("crate", right_x0, right_x1, top, floor_y)
        if exit_len:
            self.belt((right_x1, top), (right_x1 + exit_len, top), belt_speed)
        return (right_x1, top)

    def slick(self, x0, x1, top, thickness=60):
        self.block("crate", x0, x1, top, top + thickness)
        self.nodes[-1][1].append("AlwaysSlippery = true")

    def forklift(self, x, boxes, lift, period, phase=0.0):
        h = 96 * boxes
        self.moving(x, -h / 2, 96, h, (0, -lift), period, phase, look=3, base="Forklift")

    def drone(self, x, top, travel=(0, 0), period=3.0, phase=0.0, width=130):
        self.moving(x, top + 13, width, 26, travel, period, phase, look=1, base="Drone")

    def wallkick_coach(self):
        self._add("WallKickCoach", "Node2D", "kickcoach", [])

    def recorder(self):
        self._add("RunRecorder", "Node", "recorder", [])

    def coach(self):
        self._add("RolloutCoach", "Node2D", "coach", [])

    def slab(self, x0, x1, y, thickness=40):
        self.block("concrete", x0, x1, y, y + thickness)

    def kill_rect(self, x0, x1, top, bottom):
        self._add("KillZone", "Area2D", "killzone", [f"position = {vec((x0 + x1) / 2, (top + bottom) / 2)}"])
        self.nodes[-1][1].append(f"TriggerSize = {vec(x1 - x0, bottom - top)}")

    def kill_zone(self):
        width = self.right - self.left + 400
        self._add("KillZone", "Area2D", "killzone", [f"position = {vec((self.left + self.right) / 2, self.bottom + 450)}"])
        self.nodes[-1][1].append(f"TriggerSize = {vec(width, 100)}")

    def write(self, spawn_x):
        used = " ".join(line for _, lines in self.nodes for line in lines)
        resources = [r for r in RESOURCES if r[2] not in OPTIONAL_RESOURCES or f'ExtResource("{r[2]}")' in used]
        out = [f"[gd_scene load_steps={len(resources) + 1} format=3]", ""]
        for type_, path, rid in resources:
            out.append(f'[ext_resource type="{type_}" path="{path}" id="{rid}"]')
        out += [
            "",
            f'[node name="{self.scene_name}" type="Node2D"]',
            'script = ExtResource("race")',
            f'LevelId = "{self.level_id}"',
            f"CameraBounds = Rect2i({self.left - 60}, {self.top - 200}, {self.right - self.left + 120}, {self.bottom - self.top + 420})",
            "",
            '[node name="Background" type="Parallax2D" parent="."]',
            "z_index = -10",
            "scroll_scale = Vector2(0.6, 0.85)",
            "",
            '[node name="Warehouse" type="Node2D" parent="Background"]',
            'script = ExtResource("background")',
        ]
        if self.bottom > 0:
            out.append(f"Depth = {self.bottom + 1200}")
        if self.top < -1000:
            out.append(f"Height = {-self.top + 800}")
        out += [
            "",
            '[node name="Course" type="Node2D" parent="."]',
            "",
        ]
        dock_and_clocks = [n for n in self.nodes if n[0] == "." and ("LoadingDock" in n[1][0] or "Clock" in n[1][0] or "Sign" in n[1][0])]
        course = [n for n in self.nodes if n[0] == "Course"]
        crates = [n for n in course if "Crate" in n[1][0]]
        concrete = [n for n in course if "Concrete" in n[1][0] or "Belt" in n[1][0]]
        rest = [n for n in self.nodes if n[0] == "." and n not in dock_and_clocks]
        for _, lines in dock_and_clocks + crates + concrete + rest:
            out += lines + [""]
        out += [
            '[node name="Player" parent="." instance=ExtResource("player")]',
            f"position = {vec(spawn_x, -26)}",
            "",
        ]
        (ROOT / f"{self.scene_name}.tscn").write_text("\n".join(out), encoding="utf-8", newline="\n")


def level01():
    lv = Level("Level01", "level01", -700, 3100, -1000)
    lv.floor(-700, 3100)
    lv.walls()
    lv.clock(-300)
    lv.sign(-80, "Jump\nSpace")
    lv.block("crate", 150, 250, -96)
    lv.sign(450, "Double Jump\njump again in the air")
    lv.block("crate", 650, 770, -192)
    lv.sign(1060, "Slide Jump\nrun, briefly turn around, jump\njump again at the top")
    lv.block("crate", 1370, 1670, -288)
    lv.checkpoint(1780)
    lv.sign(1930, "Wall Kick\nat the wall, steer away\nand jump")
    lv.block("crate", 2150, 2230, -620, -150)
    lv.block("crate", 2370, 2450, -560)
    lv.package(2410, -600)
    lv.dock(2750)
    lv.clock(2950, clock_out=True)
    lv.write(spawn_x=-500)


def level02():
    lv = Level("Level02", "level02", -700, 3200, -1000)
    lv.floor(-700, 3200)
    lv.walls()
    lv.clock(-300)
    lv.sign(-60, "Roll\naction key while running\nrolls under low shelves")
    lv.block("crate", 200, 500, -900, -40)
    lv.sign(680, "Rollout\njump over the crate,\naction key just before landing")
    lv.block("crate", 900, 960, -60)
    lv.block("crate", 1250, 1650, -900, -40)
    lv.sign(1800, "Long shelf\nyou keep rolling in the tunnel")
    lv.block("crate", 1950, 2550, -900, -40)
    lv.package(2670, -40)
    lv.dock(2850)
    lv.clock(3050, clock_out=True)
    lv.write(spawn_x=-500)


def level03():
    lv = Level("Level03", "level03", -700, 3600, -900)
    lv.floor(-700, 400)
    lv.floor(1000, 2420)
    lv.floor(2650, 3600)
    lv.walls()
    lv.clock(-300)
    lv.sign(-90, "Dash\nShift / J in the air\nbig gap: jump + double jump + dash")
    lv.checkpoint(1050)
    lv.sign(1230, "Slide Jump + Double Jump")
    lv.block("crate", 1500, 1900, -288)
    lv.checkpoint(2010)
    lv.sign(2060, "Low ceiling\njump + dash")
    lv.block("concrete", 2200, 2900, -1000, -90)
    lv.kill_rect(2420, 2650, 40, 140)
    lv.checkpoint(2720)
    lv.package(3000, -40)
    lv.dock(3250)
    lv.clock(3450, clock_out=True)
    lv.kill_zone()
    lv.write(spawn_x=-500)


def level04():
    lv = Level("Level04", "level04", -700, 3500, -900)
    lv.floor(-700, 400)
    lv.floor(1240, 1740)
    lv.floor(2580, 3500)
    lv.walls()
    lv.clock(-300)
    lv.sign(-60, "Roll Jump → Power Dash → Jump\nroll while running, jump,\ndash, jump again")
    lv.checkpoint(250)
    lv.checkpoint(1300)
    lv.sign(1490, "Dash Rollout\nroll right as you land\nand jump again")
    lv.package(2720, -40)
    lv.dock(3000)
    lv.clock(3250, clock_out=True)
    lv.kill_zone()
    lv.write(spawn_x=-500)


def level06():
    lv = Level("Level06", "level06", -700, 5800, -1000)
    lv.floor(-700, 2500)
    lv.floor(3700, 5800)
    lv.walls()
    lv.clock(-300)
    lv.sign(-60, "AI warehouse robots\ndon't bump into them,\njumping on top is fine")
    lv.robot(250, 300, 3.2)
    lv.robot(900, 350, 2.4, 0.5)
    lv.checkpoint(1400)
    lv.block("crate", 1610, 1710, -220, -180)
    lv.sign(1470, "Barcode scanners\nred = scanned = sent back\ntip: ride the green wave")
    lv.scanner(1750, 320, 0.9, 1.1, 0.0)
    lv.scanner(1950, 320, 0.9, 1.1, 0.706)
    lv.scanner(2150, 320, 0.9, 1.1, 0.412)
    lv.checkpoint(2350)
    lv.sign(2420, "Delivery drones\nplease don't feed")
    lv.moving(2710, -60, 120, 24, (0, -200), 2.4, 0.0, look=1, base="Drone")
    lv.moving(3010, -200, 120, 24, (300, 0), 3.0, 0.25, look=1, base="Drone")
    lv.moving(3510, -100, 120, 24, (0, -200), 2.0, 0.5, look=1, base="Drone")
    lv.checkpoint(3780)
    lv.sign(3900, "The express package\nisn't in the mood today")
    lv.block("crate", 4250, 4350, -192)
    lv.block("crate", 4600, 4700, -384)
    lv.block("crate", 4900, 5000, -192)
    lv.fleeing_package(4080, -40, [(4300, -260), (4650, -480), (4950, -200)])
    lv.dock(5300)
    lv.clock(5550, clock_out=True)
    lv.kill_zone()
    lv.write(spawn_x=-500)


def level07():
    lv = Level("Level07", "level07", -700, 9100, -1000)
    lv.floor(-700, 300)
    lv.floor(1500, 2100)
    lv.floor(6250, 7400)
    lv.floor(8350, 9100)
    lv.walls()
    lv.clock(-300)
    lv.sign(-60, "Friends Ideas\naim, jump, be amazed")
    lv.block("crate", 450, 560, -60, -20)
    lv.block("crate", 740, 850, -120, -80)
    lv.moving(1000, -60, 120, 40, (300, 0), 3.0)
    lv.checkpoint(1560)
    lv.sign(1800, "Speed Jump\njump far, the belt\nflings you further")
    exit1 = lv.speed_jump(2400, -40, 450)
    exit2 = lv.speed_jump(3600, -40, 450)
    lv.block("crate", 4750, 4900, -120, 380)
    exit3 = lv.speed_jump(5150, -40, 450)
    lv.checkpoint(6320)
    lv.launch_ramp(6500, (380, -1450))
    lv.block("crate", 6950, 7250, -420, -380)
    lv.package(7100, -460)
    lv.sign(7330, "Bubble wrap\nPOP!")
    lv.bubble_pad(7480, 0)
    lv.bubble_pad(7850, 0)
    lv.bubble_pad(8220, 0)
    lv.dock(8600)
    lv.clock(8850, clock_out=True)
    lv.kill_zone()
    lv.write(spawn_x=-500)
    print("speed jump exits", exit1, exit2, exit3)


def level08():
    lv = Level("Level08", "level08", -700, 8400, -700, bottom=1500)
    lv.walls()
    lv.floor(-700, 300)
    lv.clock(-300)
    lv.sign(-60, "Slip 'n' Slide\ntilted crates = boost")
    end1 = lv.slide(360, 120, 700, 33)
    lv.floor(end1[0], 1350, end1[1])
    lv.belt((1600, end1[1]), (2000, end1[1]), 300)
    lv.belt((2000, end1[1]), (2346, end1[1] - 200), 350)
    top2 = end1[1] - 200
    lv.floor(2346, 2500, top2)
    lv.checkpoint(2420, top2)
    lv.belt((2500, top2), (2950, top2), -350)
    lv.sign(2420, "Reverse belt\njump over it!", top2)
    lv.floor(2950, 3300, top2)
    end2 = lv.slide(3360, top2 + 120, 900, 36)
    lv.floor(end2[0], 4300, end2[1])
    lv.checkpoint(4200, end2[1])
    launch = (4710, end2[1] - 287)
    lv.belt((4300, end2[1]), launch, 400)
    end3 = lv.slide(4900, launch[1] + 140, 700, 32)
    lv.floor(end3[0], 5800, end3[1])
    lv.checkpoint(5700, end3[1])
    lv.sign(5560, "Up or down?\nboth ways are fast", end3[1])
    fork = end3[1]
    upper_start = (5950, fork)
    upper_top = (6358, fork - 190)
    lv.belt(upper_start, upper_top, 350)
    lv.belt(upper_top, (6900, upper_top[1]), 400)
    lv.slide(6900, upper_top[1], 640, 30)
    lower_end = lv.slide(5900, fork + 170, 600, 35)
    lv.belt(lower_end, (7000, lower_end[1]), 450)
    final_y = fork + 360
    lv.belt((7000, lower_end[1]), (7290, final_y), 350)
    lv.floor(7290, 8400, final_y)
    lv.package(7950, final_y - 40)
    lv.dock(8130, final_y)
    lv.clock(8310, clock_out=True, y=final_y)
    lv.kill_zone()
    lv.write(spawn_x=-500)
    print("level08 points", end1, top2, end2, launch, end3, lower_end, final_y)


def level09():
    lv = Level("Level09", "level09_hochregal", -800, 6600, -1700)
    lv.walls()
    lv.floor(-800, 6600)
    lv.clock(-400)
    lv.sign(-150, "High-bay warehouse\nright, back, then across the top")
    lv.block("crate", 200, 300, -96)
    lv.belt((500, -4), (1000, -4), 300)
    lv.checkpoint(1150)
    lv.block("crate", 1300, 1420, -192)
    lv.belt((1700, -4), (2200, -4), -340)
    lv.checkpoint(2350)
    lv.block("crate", 2500, 2620, -192)
    lv.slide(2618.4991, -192.8137, 426.7, 26.742175)
    lv.checkpoint(3250)
    lv.sign(3330, "Wide chimney\nwall kick + double jump")
    f1 = -700
    f2 = -1300
    lv.block("crate", 3500, 3580, f1 - 60, -140)
    lv.block("crate", 3880, 3960, f2 + 40, 0)
    lv.checkpoint(3540, f1 - 60)
    lv.slab(2550, 3500, f1)
    lv.slab(1000, 2300, f1)
    lv.belt((2800, f1 - 4), (3400, f1 - 4), -350)
    lv.checkpoint(2700, f1)
    lv.block("crate", 2000, 2100, f1 - 192, f1)
    lv.slide(1654.0245, -700.2496, 396.1, -28.997680)
    lv.belt((1300, f1 - 4), (1600, f1 - 4), 340)
    lv.checkpoint(1150, f1)
    lv.belt((200, f2), (1000, f1), -300)
    lv.slab(-400, 200, f2)
    lv.slab(420, 1500, f2)
    lv.slab(1750, 4300, f2)
    lv.checkpoint(300, f2)
    lv.block("crate", -350, -250, f2 - 192, f2)
    lv.block("crate", -150, -50, f2 - 400, f2 - 360)
    lv.package(-100, f2 - 440)
    lv.belt((700, f2 - 4), (1300, f2 - 4), 400)
    lv.checkpoint(1850, f2)
    lv.block("crate", 2000, 2120, f2 - 192, f2)
    lv.slide(2118.4477, -1492.9047, 400.9, 28.604058)
    lv.checkpoint(2900, f2)
    lv.belt((3100, f2 - 4), (3500, f2 - 4), -350)
    lv.checkpoint(4150, f2)
    lv.sign(4000, "Down you go!", f2)
    end1 = lv.slide(4300, f2, 900, 38)
    end2 = lv.slide(end1[0] + 70, end1[1] + 60, 800, 38)
    lv.checkpoint(5900)
    lv.dock(6100)
    lv.clock(6350, clock_out=True)
    lv.write(spawn_x=-600)
    print("level09 slides", end1, end2)


def level10():
    lv = Level("Level10", "level10", -800, 12300, -1500)
    lv.walls()
    lv.floor(-800, 900)
    lv.clock(-400)
    lv.sign(-150, "Climbing hall\nwall kick, wall kick, belt!")
    exit1 = lv.chimney(600, 0, 520, 140, exit_len=600)
    lv.checkpoint(1000, exit1[1])
    lv.slab(exit1[0] + 600, 2200, exit1[1])
    exit2 = lv.chimney(1900, exit1[1], 480, 140, exit_len=600)
    lv.checkpoint(2300, exit2[1])
    lv.sign(2500, "Speed jumps\ndown, flat, up", exit2[1])
    sj1 = lv.speed_jump(2950, -900, 450)
    sj2 = lv.speed_jump(4250, -800, 450)
    sj3 = lv.speed_jump(5450, -700, 450)
    lv.slab(6550, 8200, -500, 200)
    lv.checkpoint(6650, -500)
    lv.launch_ramp(7000, (380, -1450))
    lv.add_launch_y(-500)
    lv.slab(7450, 8200, -920)
    lv.checkpoint(7600, -920)
    exit3 = lv.chimney(7900, -920, 450, 140, exit_len=600)
    lv.checkpoint(8300, exit3[1])
    end1 = lv.slide(8900, exit3[1] + 70, 900, 35)
    end2 = lv.slide(9750, end1[1] + 90, 900, 35)
    lv.floor(end2[0], 12300, end2[1])
    lv.checkpoint(end2[0] + 100, end2[1])
    lv.block("crate", 11350, 11450, end2[1] - 192, end2[1])
    lv.package(11400, end2[1] - 232)
    lv.dock(11800, end2[1])
    lv.clock(12050, clock_out=True, y=end2[1])
    lv.kill_zone()
    lv.write(spawn_x=-600)
    print("level10 points", exit1, exit2, sj1, sj2, sj3, exit3, end1, end2)


def level11():
    lv = Level("Level11", "level11", -700, P["end"], -1800)
    lv.walls()
    lv.floor(-700, 400)
    lv.clock(-300)
    lv.sign(-60, "Rhythm line\njump in rhythm")
    for i in range(6):
        x0 = P["box0"] + i * P["box_step"]
        lv.block("crate", x0, x0 + 110, P["box_top"], 400)
    run_up = P["box0"] + 5 * P["box_step"] + 110 + P["box_gap_end"]
    lv.floor(run_up, P["sj1"] - P["sj1_gap"])
    lv.checkpoint(run_up + 80)
    lv.sign(run_up + 300, "Speed Jump\ninto the chimney!")
    sj1 = lv.speed_jump(P["sj1"], -40, 450)
    c1 = P["c1"]
    c1_top = P["c1_top"]
    lv.block("crate", c1 - 80 - P["c1_gap"], c1 - P["c1_gap"], c1_top - 20, P["c1_left_bottom"])
    lv.block("crate", c1, c1 + 80, c1_top, 0)
    lv.floor(c1 - 220, c1)
    lv.checkpoint(c1 - 70)
    lv.checkpoint(c1 + 40, c1_top)
    sj2 = lv.speed_jump(P["sj2"], P["sj2_y"], 450)
    pad = P["pad"]
    lv.bubble_pad(pad, P["pad_top"], 260)
    c2 = P["c2"]
    c2_top = P["c2_top"]
    lv.block("crate", c2 - 80 - P["c2_gap"], c2 - P["c2_gap"], c2_top - 60, P["c2_left_bottom"])
    lv.block("crate", c2, c2 + 80, c2_top, P["pad_top"] + 200)
    lv.checkpoint(c2 + 40, c2_top)
    lv.package(c2 + 160, c2_top - 40)
    belt_end = c2 + 80 + P["belt_len"]
    lv.belt((c2 + 80, c2_top), (belt_end, c2_top), 350)
    lv.sign(c2 + 300, "Gap, then tilted\nbubble wrap!", c2_top)
    lv.tilted_bubble(belt_end + P["tilt_dx"], c2_top + P["tilt_dy"], 240, 45)
    lv.floor(P["end_floor"], P["end"])
    lv.checkpoint(P["end_floor"] + 100)
    lv.dock(P["end"] - 600)
    lv.clock(P["end"] - 300, clock_out=True)
    lv.kill_zone()
    lv.write(spawn_x=-500)
    print("level11 points", sj1, sj2, belt_end)


def level12():
    lv = Level("Level12", "level12", -700, 2800, -900)
    lv.walls()
    lv.floor(-700, 2800)
    lv.clock(-300)
    lv.sign(-150, "Speed training\nstraight ahead, nothing in the way")
    lv.package(80, -40)
    lv.sign(500, "Roll, roll jump, power dash,\nrollout when landing\nand jump again right away")
    lv.sign(1250, "Keep the rhythm:\njump, dash, rollout, jump ...")
    lv.sign(820, "Optimal rotation:\njump and dash almost together\n(tiny hop), rollout on landing,\njump right out of the rollout")
    lv.sign(1800, "Even faster?\nrollout right before landing\n= perfect rollout")
    lv.dock(2200)
    lv.clock(2500, clock_out=True)
    lv.coach()
    lv.recorder()
    lv.write(spawn_x=-500)


def level13(package_y=-560):
    lv = Level("Level13", "level13", -600, 1100, -900)
    lv.walls()
    lv.floor(-600, 1100)
    lv.clock(-400)
    lv.package(-330, package_y)
    lv.sign(-150, "Top shelf\nslide jump, double jump,\nupward dash (W or up + dash)\nor off the wall")
    lv.dock(600)
    lv.clock(850, clock_out=True)
    lv.write(spawn_x=-560)


def level14():
    lv = Level("Level14", "level14", -700, 8619, -1500)
    lv.walls()
    lv.floor(-700, 696)
    lv.clock(-300)
    lv.sign(-100, "Forklift hall\nslide, ride along, roll through")
    lv.block("crate", 250, 346, -96)
    lv.block("crate", 600, 696, -288)
    down = lv.slide(696, -288, 502, 35)
    lv.slick(down[0], down[0] + 300, down[1])
    up = lv.slide(down[0] + 300, down[1], 272, -22)
    lv.floor(up[0] + 40, 3000)
    fork1 = up[0] + 450
    fork2 = fork1 + 380
    lv.checkpoint(fork1 - 200)
    lv.forklift(fork1, 1, 320, 3.2, 0.84375)
    lv.forklift(fork2, 2, 380, 3.6, 0.1111)
    p1 = fork2 + 330
    p2 = p1 + 520
    p3 = p2 + 300
    lv.drone(p1, -700, (200, 0), 3.0)
    lv.drone(p2, -700)
    lv.drone(p3, -700, (200, 0), 3.0, 0.5)
    wall = p3 + 460
    lv.block("concrete", wall - 100, wall + 240, -700, 400)
    lv.block("concrete", wall, wall + 240, -1500, -744)
    lv.checkpoint(wall - 60, -700)
    lv.sign(wall - 50, "roll through, then right away\ndouble jump + dash", -700)
    for x in (wall + 725, wall + 965, wall + 1205):
        lv.drone(x, -700)
    wall2 = wall + 1620
    lv.block("concrete", wall2 - 200, wall2 + 440, -700, 400)
    lv.block("concrete", wall2, wall2 + 240, -1500, -744)
    lv.checkpoint(wall2 - 100, -700)
    lv.sign(wall2 - 170, "roll through\nthe gap", -700)
    lv.checkpoint(wall2 + 340, -700)
    lv.sign(wall2 + 300, "Double jump +\ndiagonal dash upward", -700)
    for x in (wall2 + 640, wall2 + 880, wall2 + 1120):
        lv.drone(x, -990)
    lv.slab(wall2 + 1300, wall2 + 1600, -990)
    lv.checkpoint(wall2 + 1350, -990)
    lv.package(wall2 + 1450, -1030)
    lv.slab(wall2 + 2120, wall2 + 2900, -990)
    lv.dock(wall2 + 2350, -990)
    lv.clock(wall2 + 2650, clock_out=True, y=-990)
    lv.kill_zone()
    lv.write(spawn_x=-500)
    print("level14 points", down, up, fork1, fork2, p1, p2, p3, wall)


def level15():
    lv = Level("Level15", "level15", -700, 2100, -1100)
    lv.walls()
    lv.floor(-700, 1340)
    lv.clock(-300)
    lv.sign(-150, "Wall kick training\ncome in fast, kick right on contact:\npress jump just before the wall")
    lv.belt((0, -4), (980, -4), 500)
    lv.sign(780, "jump in, then kick\nfrom wall to wall")
    left, right, top = 900, 1260, -625
    lv.block("crate", left, left + 80, top - 300, -220)
    lv.block("crate", right, right + 80, top, 0)
    lv.slab(right + 80, 2100, top)
    lv.package(right + 220, top - 40)
    lv.dock(right + 440, top)
    lv.clock(right + 690, clock_out=True, y=top)
    lv.wallkick_coach()
    lv.recorder()
    lv.kill_zone()
    lv.write(spawn_x=-500)


P = dict(end=10100, box0=520, box_step=240, box_top=-60, box_gap_end=170, sj1=2950, sj1_gap=250,
         c1=4120, c1_gap=280, c1_top=-900, c1_left_bottom=-320, sj2=4270, sj2_y=-720,
         pad=5910, pad_top=-600, c2=6290, c2_gap=140, c2_top=-1500, c2_left_bottom=-940,
         belt_len=600, tilt_dx=250, tilt_dy=250, end_floor=7560)


import sys as _sys
if len(_sys.argv) > 1:
    for _name in _sys.argv[1:]:
        _fn, *_args = _name.split("=")
        globals()[_fn](*[int(a) for a in _args])
    raise SystemExit

level01()
level02()
level03()
level04()
level06()
level07()
level08()
level09()
level10()
level11()
level12()
level13()
level14()
level15()
print("ok")
