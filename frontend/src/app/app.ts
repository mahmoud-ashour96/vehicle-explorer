import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { VehicleExplorer } from './vehicles/vehicle-explorer/vehicle-explorer';

@Component({
  selector: 'app-root',
  imports: [MatIconModule, VehicleExplorer],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
