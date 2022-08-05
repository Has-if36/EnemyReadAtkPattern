# Enemy’s Reaction to Player’s Attack using Reinforcement Learning (RL) on Unity

## Introduction
Most games that have fighting elements usually have many kinds of attack pattern. The purpose for this is to have better gameplay experience. Some games have a mechanic where the enemy can block or evade player’s attack.
Using probability to make enemy dodge is really common. However, there is a is not much factor that can affect this randomness. You can however add some elements to affect this randomness such as Accuracy Stats or Agility Stats, but that's not the point for this project. Therefore, I would like to make an enemy that will dodge based on player's attack pattern. Player's attack pattern will be the factor which will afect enemy's behaviour to dodge.

## Why using Reinforcement Learning?
When you want to implement AI in games, you have to make sure the algorithm is efficient so that player can enjoy smoother game experience. Reinforcement Learning is quite efficient, at least when comparing with Neural Networ or Deep Learning. The model itself is also fairly easy to understand compared to other AI Algorithm and the implementation is also not very difficult.

## RL Component
| Component | Value |
| ------------- | ------------- |
| Agent | Enemy |
| Environment | The game itself |
| Observation | Player’s Attack Pattern |
| Action | Enemy’s Prediction on Player’s Attack Pattern |
| Reward&nbsp | <ul><li>Right Guess Seen</li><li>Player’s Current Attack Pattern</li></ul> |
| Punishment | <ul><li>Wrong Guess</li></ul> |

## Result
### Probability VS Reinforcement Learning
<div align="center">
Probability (50% Dodge Rate) |  Reinforcement Learning
:-------------------------:|:-------------------------:
![EvadeProb.gif](https://user-images.githubusercontent.com/55189926/183035363-bce456f8-fb01-4121-9ea2-7be428e3334e.gif)  |  ![EvadeRL.gif](https://user-images.githubusercontent.com/55189926/183035378-4975ea12-a976-485f-8bcd-8a1e8f8e0069.gif)
</div>
  
### Beating Enemy with RL

