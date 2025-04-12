local dir = true
local minY = -300
local maxY = -25

function doSomething(first, other)
    local pos = first.position

    if dir then
        pos.y = pos.y + 10
        if pos.y >= maxY then
            dir = false
        end
    else
        pos.y = pos.y - 10
        if pos.y <= minY then
            dir = true
        end
    end

    first.position = pos
end