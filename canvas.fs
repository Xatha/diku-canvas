module Canvas
open System.Runtime.InteropServices
open System

// colors
type color = {r:byte;g:byte;b:byte;a:byte}

let fromArgb (r:int,g:int,b:int,a:int) : color =
  {r=byte(r); g=byte(g); b=byte(b); a=byte(a)}

let fromRgb (r:int,g:int,b:int) : color = fromArgb (r,g,b,255)

let red : color = fromRgb(255,0,0)
let green : color = fromRgb(0,255,0)
let blue : color = fromRgb(0,0,255)
let yellow : color = fromRgb(255,255,0)
let lightgrey : color = fromRgb (220,220,220)
let white : color = fromRgb (255,255,255)
let black : color = fromRgb (0,0,0)

let fromColor (c: color) : int * int * int * int =
  (int(c.r),int(c.g),int(c.b),int(c.a))

// canvas - 4 bytes for each pixel (r,g,b,a)
type canvas = {w:int;h:int;data:byte[]}

// create white opaque canvas
let create (w:int) (h:int) : canvas =
  {w=w;
   h=h;
   data=Array.create (h*w*4) 0xffuy}

let height (C:canvas) = C.h
let width (C:canvas) = C.w

// draw a single pixel if it is inside the bitmap
let setPixel (C:canvas) (c: color) (x:int,y:int) : unit =
  if x < 0 || y < 0 || x >= C.w || y >= C.h then ()
  else 
    let i = 4*(y*C.w+x)
    // Even though SDL_PIXELFORMAT is RGBA8888
    // We need to structure it in reverse, so in reality ABGR
    C.data.[i]   <- c.a;
    C.data.[i+1] <- c.b;
    C.data.[i+2] <- c.g;
    C.data.[i+3] <- c.r;

// draw a line by linear interpolation
let setLine (C:canvas) (c: color) (x1:int,y1:int) (x2:int,y2:int) : unit =
  let m = max (abs(y2-y1)) (abs(x2-x1))
  if m = 0 then ()
  else 
    for i in [0..m] do
    let x = ((m-i)*x1 + i*x2) / m
    let y = ((m-i)*y1 + i*y2) / m
    setPixel C c (x,y)

// draw a box
let setBox (C:canvas) (c:color) (x1:int,y1:int) (x2:int,y2:int) : unit =
  do setLine C c (x1,y1) (x2,y1)
  do setLine C c (x2,y1) (x2,y2)
  do setLine C c (x2,y2) (x1,y2)
  do setLine C c (x1,y2) (x1,y1)

let setFillBox (C:canvas) (c:color) (x1:int,y1:int) (x2:int,y2:int) : unit =
  for x in [x1..x2] do
    for y in [y1..y2] do
      do setPixel C c (x,y)

// get a pixel color from a bitmap
let getPixel (C:canvas) (x:int,y:int) : color =   // rgba
  let i = 4*(y*C.w+x)
  {a=C.data.[i]; 
   b=C.data.[i+1];
   g=C.data.[i+2]; 
   r=C.data.[i+3]}

// initialize a new bitmap
let init (w:int) (h:int) (f:int*int->color) : canvas =    // rgba
  let data = Array.create (h*w*4) 0xffuy
  for y in [0..h-1] do
    for x in [0..w-1] do
      let c = f (x,y)
      let i = 4*(y*w+x)
      data.[i] <- c.a;
      data.[i+1] <- c.b;
      data.[i+2] <- c.g;
      data.[i+3] <- c.r
  {h=h;w=w;data=data}

// scale a bitmap
let scale (C:canvas) (w2:int) (h2:int) : canvas =
  let scale_x x = (x * C.w) / w2
  let scale_y y = (y * C.h) / h2
  init w2 h2 (fun (x,y) -> getPixel C (scale_x x, scale_y y))



// Files
// read a bitmap file
// TODO: Add exception handling in case SDL2_image is not found or fails to load
// TODO: Add exception handling in case the file didn't exist and surfacePtr = null
let fromFile (fname : string) : canvas =
    // Grab a sweet-ass pointer to a C-struct, yeehaw
    let surfacePtr = SDLImage.IMG_Load(fname)
    // Copy the struct to managed memory
    let surface = Marshal.PtrToStructure<SDLImage.SDL_Surface>(surfacePtr)
    // Width in pixels
    let w = surface.w
    // Height in pixels
    let h = surface.h
    // Amount of bytes per row
    let p = surface.pitch
    let totalBytes = p * h
    let data = Array.create totalBytes 0x00uy
    Marshal.Copy(surface.pixels, data, 0, totalBytes)
    // Free the surface, so we only have managed memory left
    SDL.SDL_FreeSurface(surfacePtr) |> ignore
    // Construct a Canvas and return it
    {w=w; h=h; data=data}

// create a pixbuf from a bitmap
let toPixbuf (C:canvas) =
  failwith "Not implemented"

// save a bitmap as a png file
let toPngFile (C:canvas) (fname : string) : unit =
    let w = C.w
    let h = C.h
    let pixels = C.data
    let pixelsPtr = IntPtr ((Marshal.UnsafeAddrOfPinnedArrayElement (pixels, 0)).ToPointer ())
    let surface = SDL.SDL_CreateRGBSurfaceFrom (pixelsPtr, w, h, 32, 4*w, 0xFF000000u, 0x00FF0000u, 0x0000FF00u, 0x000000FFu)
    match SDLImage.IMG_SavePNG(surface, fname) with
        | 0 -> ()
        | _ -> printfn "Error when saving image to path %A" fname

    SDL.SDL_FreeSurface(surface) |> ignore


// start and run an application with an action
let runApplication (action:unit -> unit) =
  failwith "not implemented"

type key = Keysym of int

type ImgUtilKey =
    | Unknown
    | DownArrow
    | UpArrow
    | LeftArrow
    | RightArrow
    | Space

let getKey (k:key) : ImgUtilKey =
    let kval = match k with | Keysym k -> uint32 k
    match kval with
        | _ when kval = SDL.SDLK_SPACE -> Space
        | _ when kval = SDL.SDLK_UP -> UpArrow
        | _ when kval = SDL.SDLK_DOWN -> DownArrow
        | _ when kval = SDL.SDLK_LEFT -> LeftArrow
        | _ when kval = SDL.SDLK_RIGHT -> RightArrow
        | _ -> Unknown

// start an app that can listen to key-events
let runApp (t:string) (w:int) (h:int)
           (f:int -> int -> 's -> canvas)
           (onKeyDown: 's -> key -> 's option) (s:'s) : unit =

    let state = ref s

    SDL.SDL_Init(SDL.SDL_INIT_VIDEO) |> ignore

    let viewWidth, viewHeight = w, h
    let mutable window, renderer = IntPtr.Zero, IntPtr.Zero
    let windowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN |||
                      SDL.SDL_WindowFlags.SDL_WINDOW_INPUT_FOCUS

    SDL.SDL_CreateWindowAndRenderer(viewWidth, viewHeight, windowFlags, &window, &renderer) |> ignore
    let texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888, SDL.SDL_TEXTUREACCESS_STREAMING, viewWidth, viewHeight)

    let frameBuffer = Array.create (viewWidth * viewHeight *4 ) (byte(0))
    let bufferPtr = IntPtr ((Marshal.UnsafeAddrOfPinnedArrayElement (frameBuffer, 0)).ToPointer ())
    let mutable keyEvent = SDL.SDL_KeyboardEvent 0u

    let rec drawLoop() =
        let C = f w h (!state)
        Array.blit C.data 0 frameBuffer 0 C.data.Length

        SDL.SDL_UpdateTexture(texture, IntPtr.Zero, bufferPtr, viewWidth * 4) |> ignore
        SDL.SDL_RenderClear(renderer) |> ignore
        SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero) |> ignore
        SDL.SDL_RenderPresent(renderer) |> ignore

        let ret = SDL.SDL_WaitEvent(&keyEvent)
        if ret = 0 then () // an error happened so we exit
        else if keyEvent.``type`` = SDL.SDL_QUIT then ()
        else if (keyEvent.``type`` = SDL.SDL_KEYDOWN) then
             if keyEvent.keysym.sym = SDL.SDLK_ESCAPE then
                 () // quit the game by exiting the loop
             else
                 let k = keyEvent.keysym.sym
                 match onKeyDown (!state) (Keysym (int k)) with
                     | Some s -> state := s
                     | None   -> ()
                 drawLoop()
        else
            drawLoop()
    drawLoop ()

    SDL.SDL_DestroyTexture(texture)
    SDL.SDL_DestroyRenderer(renderer)
    SDL.SDL_DestroyWindow(window)
    SDL.SDL_Quit()
    ()

let runSimpleApp t w h (f:int->int->canvas) : unit =
  runApp t w h (fun w h () -> f w h)
               (fun _ _ -> None) ()

let show (C:canvas) (t:string) : unit =
  runApp t (width C) (height C) (fun w h () -> scale C w h) (fun _ _ -> None) ()



//////////
// Turtle commands

type turtleCmd =
  | SetColor of color
  | Turn of int       // degrees right (0-360)
  | Move of int       // 1 unit = 1 pixel
  | PenUp
  | PenDown

// Interpreter turning commands into lines

type point = int * int
type line = point * point * color

let pi = System.Math.PI

let rec interp (p:point,d:int,c:color,up:bool) (cmds : turtleCmd list) (acc:line list) : line list =
  match cmds with
    | [] -> acc
    | SetColor c :: cmds -> interp (p,d,c,up) cmds acc
    | Turn i :: cmds -> interp (p,d-i,c,up) cmds acc
    | PenUp :: cmds -> interp (p,d,c,true) cmds acc
    | PenDown :: cmds -> interp (p,d,c,false) cmds acc
    | Move i :: cmds ->
      let r = 2.0 * pi * float d / 360.0
      let dx = int(float i * cos r)
      let dy = -int(float i * sin r)
      let (x,y) = p
      let p2 = (x+dx,y+dy)
      let acc2 = if up then acc else (p,p2,c)::acc
      interp (p2,d,c,up) cmds acc2

let turtleDraw (w:int,h:int) (title:string) (pic:turtleCmd list) : unit =
  let C = create w h
  let center = (w/2,h/2)
  let dir_up = 90
  let initState = (center,dir_up,black,false)
  let lines = interp initState pic []
  for (p1,p2,c) in lines do
    do setLine C c p1 p2
  do show C title