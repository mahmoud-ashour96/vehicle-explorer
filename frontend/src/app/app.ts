import { Component } from '@angular/core';
import { VehicleExplorer } from './vehicle-explorer/vehicle-explorer';

@Component({
  selector: 'app-root',
  imports: [VehicleExplorer],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}
