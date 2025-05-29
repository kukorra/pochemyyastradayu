program Test4;
var
  x, y: integer;
begin
  x = 5;
  y = x...10;
  case x of
    1: y := 1;
    2...3: y := 2;
    4...5...6: y := 3;
    k: y := 4;
  end;
end.