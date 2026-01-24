<template>
    <div></div>
    <div>
        <LeaderboardRow firstCol="Rank" secondCol="Name" thirdCol="Points" header/>
        <LeaderboardRow
            v-for="data in datas"
            :key="data.no"
            :firstCol="data.no"
            :secondCol="data.name"
            :thirdCol="data.points"
        />
    </div>
</template>

<script setup lang="ts">
import LeaderboardRow from './LeaderboardRow.vue';
const datas = [
    {
        no: '1',
        name: 'Nico Abel',
        points: '180'
    },
      {
        no: '2',
        name: 'Nico Abel',
        points: '180'
    },
      {
        no: '3',
        name: 'Nico Abel',
        points: '180'
    },
]
</script>